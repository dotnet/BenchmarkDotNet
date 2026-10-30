using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Environments
{
    public class PhysicalMemoryInfo
    {
        public long TotalPhysicalBytes { get; }
        public long? AvailablePhysicalBytes { get; }
        public long? FrequencyMts { get; }
        public string? MemoryType { get; }

        public PhysicalMemoryInfo(long totalPhysicalBytes, long? availablePhysicalBytes = null)
            : this(totalPhysicalBytes, availablePhysicalBytes, frequencyMts: null, memoryType: null)
        {
        }

        public PhysicalMemoryInfo(long totalPhysicalBytes, long? availablePhysicalBytes, long? frequencyMts, string? memoryType)
        {
            TotalPhysicalBytes = totalPhysicalBytes;
            AvailablePhysicalBytes = availablePhysicalBytes;
            FrequencyMts = frequencyMts;
            MemoryType = memoryType;
        }

        public string ToFormattedString()
        {
            double totalGb = TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            var sb = new StringBuilder();
            sb.Append($"{Math.Round(totalGb, 2)} GB");

            if (AvailablePhysicalBytes.HasValue)
                sb.Append(" Total");

            if (MemoryType.IsNotBlank() || FrequencyMts.HasValue)
            {
                sb.Append(" (");
                if (MemoryType.IsNotBlank())
                    sb.Append(MemoryType);
                if (FrequencyMts.HasValue)
                {
                    if (MemoryType.IsNotBlank())
                        sb.Append('-');
                    sb.Append($"{FrequencyMts}MT/s");
                }
                sb.Append(')');
            }

            if (AvailablePhysicalBytes.HasValue)
            {
                double availableGb = AvailablePhysicalBytes.Value / (1024.0 * 1024.0 * 1024.0);
                sb.Append($", {Math.Round(availableGb, 2)} GB Available");
            }

            return sb.ToString();
        }
    }

    public static class SystemMemory
    {
        private static readonly TimeSpan SystemProfilerTimeout = TimeSpan.FromSeconds(5);

        private static readonly Regex MemInfoValueRegex = new(@"[0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static PhysicalMemoryInfo? GetPhysicalMemory()
        {
            try
            {
                if (OsDetector.IsWindows7OrLater())
                    return GetWindowsMemory();

                if (OsDetector.IsLinux())
                    return GetLinuxMemory();

                if (OsDetector.IsMacOS())
                    return GetMacMemory();
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return null;
        }

        [SupportedOSPlatform("windows6.0.6000")]
        private static PhysicalMemoryInfo? GetWindowsMemory()
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint) Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (PInvoke.GlobalMemoryStatusEx(ref memStatus))
            {
                var (speed, type) = GetWindowsMemoryDetails();
                return new PhysicalMemoryInfo((long) memStatus.ullTotalPhys, (long) memStatus.ullAvailPhys, speed, type);
            }

            return null;
        }

        private static PhysicalMemoryInfo? GetLinuxMemory()
        {
            const string path = "/proc/meminfo";
            if (!File.Exists(path))
                return null;

            var (total, available) = ParseMemInfo(File.ReadAllLines(path));
            if (total <= 0)
                return null;

            var (speed, type) = GetLinuxMemoryDetails();
            return new PhysicalMemoryInfo(total, available, speed, type);
        }

        internal static (long TotalBytes, long? AvailableBytes) ParseMemInfo(IEnumerable<string> lines)
        {
            long total = 0;
            long? available = null;
            long? free = null;

            foreach (var line in lines)
            {
                if (!TryParseLine(line, out string field, out long bytes))
                    continue;

                switch (field)
                {
                    case "MemTotal":
                        total = bytes;
                        break;
                    case "MemAvailable":
                        available = bytes;
                        break;
                    case "MemFree":
                        free = bytes;
                        break;
                }
            }

            // MemFree is used only as a fallback when MemAvailable is not available.
            return (total, available ?? free);

            static bool TryParseLine(string line, out string field, out long bytes)
            {
                field = "";
                bytes = 0;

                int separator = line.IndexOf(':');
                if (separator <= 0)
                    return false;

                var match = MemInfoValueRegex.Match(line, separator + 1);
                if (match.Success && long.TryParse(match.Value, out long kb))
                {
                    field = line.Substring(0, separator);
                    bytes = kb * 1024;
                    return true;
                }

                return false;
            }
        }

        private static (long? SpeedMts, string? MemoryType) GetLinuxMemoryDetails()
        {
            // The raw SMBIOS table is readable only by root on most distros.
            try
            {
                const string dmiTablePath = "/sys/firmware/dmi/tables/DMI";
                if (File.Exists(dmiTablePath))
                {
                    var details = ParseSmbiosMemoryDevices(File.ReadAllBytes(dmiTablePath));
                    if (details.SpeedMts.HasValue || details.MemoryType != null)
                        return details;
                }
            }
            catch
            {
                // Fall back to EDAC
            }

            // EDAC is world-readable but usually only present on ECC systems, and it does not expose the speed.
            try
            {
                const string edacPath = "/sys/devices/system/edac/mc";
                if (Directory.Exists(edacPath))
                {
                    foreach (var mcDir in Directory.GetDirectories(edacPath, "mc*"))
                    {
                        foreach (var dimmDir in Directory.GetDirectories(mcDir, "dimm*"))
                        {
                            string typePath = Path.Combine(dimmDir, "dimm_mem_type");
                            if (File.Exists(typePath))
                            {
                                string? memType = NormalizeEdacMemoryType(File.ReadAllText(typePath).Trim());
                                if (memType != null)
                                    return (null, memType);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore fallback failures
            }

            return (null, null);
        }

        /// <summary>
        /// Maps a kernel EDAC memory type name (e.g. "Registered-DDR4", "Low-Power-DDR5-RAM") to the plain memory type ("DDR4", "LPDDR5").
        /// </summary>
        internal static string? NormalizeEdacMemoryType(string edacType)
        {
            var match = Regex.Match(edacType, @"DDR\d*");
            if (!match.Success)
                return null;

            return edacType.StartsWith("Low-Power", StringComparison.Ordinal) ? "LP" + match.Value : match.Value;
        }

        [SupportedOSPlatform("windows6.0.6000")]
        private static unsafe (long? SpeedMts, string? MemoryType) GetWindowsMemoryDetails()
        {
            try
            {
                const uint rsMb = 0x52534D42; // 'RSMB'
                var provider = (FIRMWARE_TABLE_PROVIDER) rsMb;
                uint bufferSize = PInvoke.GetSystemFirmwareTable(provider, 0, null, 0);
                if (bufferSize == 0)
                    return (null, null);

                byte[] buffer = new byte[bufferSize];
                fixed (byte* pBuffer = buffer)
                {
                    if (PInvoke.GetSystemFirmwareTable(provider, 0, pBuffer, bufferSize) == 0)
                        return (null, null);
                }

                // RawSMBIOSData header: 4 version bytes, then the DWORD length of the structure table that follows.
                const int headerSize = 8;
                if (buffer.Length < headerSize)
                    return (null, null);
                int tableLength = (int) Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(4)), (uint) (buffer.Length - headerSize));
                return ParseSmbiosMemoryDevices(buffer.AsSpan(headerSize, tableLength));
            }
            catch
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Reads the memory type and speed from the Memory Device (type 17) structures of an SMBIOS structure table (DMTF DSP0134).
        /// </summary>
        internal static (long? SpeedMts, string? MemoryType) ParseSmbiosMemoryDevices(ReadOnlySpan<byte> table)
        {
            const byte memoryDeviceType = 17;
            const byte endOfTableType = 127;

            long? speed = null;
            string? memoryType = null;
            int offset = 0;

            while (offset + 4 <= table.Length)
            {
                byte structureType = table[offset];
                byte length = table[offset + 1];

                if (structureType == endOfTableType || length < 4 || offset + length > table.Length)
                    break;

                if (structureType == memoryDeviceType)
                {
                    var device = table.Slice(offset, length);
                    if (IsInstalledMemoryDevice(device))
                    {
                        if (memoryType == null && device.Length > 0x12)
                            memoryType = MapSmbiosMemoryType(device[0x12]);

                        // Mixed modules all run at the speed of the slowest one.
                        long? deviceSpeed = GetMemoryDeviceSpeed(device);
                        if (deviceSpeed.HasValue && (speed == null || deviceSpeed.Value < speed.Value))
                            speed = deviceSpeed;
                    }
                }

                // Skip the formatted area, then the string-set, which ends with a double null.
                offset += length;
                while (offset + 1 < table.Length && (table[offset] != 0 || table[offset + 1] != 0))
                    offset++;
                offset += 2;
            }

            return (speed, memoryType);
        }

        // Size (WORD at 0x0C) is 0 for an empty slot.
        private static bool IsInstalledMemoryDevice(ReadOnlySpan<byte> device)
            => device.Length < 0x0E || BinaryPrimitives.ReadUInt16LittleEndian(device.Slice(0x0C)) != 0;

        // Configured Memory Speed (0x20, SMBIOS 2.7+) is what the module actually runs at.
        // Speed (0x15) is the module's maximum rated speed, used only when the configured speed is not reported.
        private static long? GetMemoryDeviceSpeed(ReadOnlySpan<byte> device)
            => ReadSmbiosSpeed(device, wordOffset: 0x20, extendedOffset: 0x58)
                ?? ReadSmbiosSpeed(device, wordOffset: 0x15, extendedOffset: 0x54);

        private static long? ReadSmbiosSpeed(ReadOnlySpan<byte> device, int wordOffset, int extendedOffset)
        {
            if (device.Length < wordOffset + 2)
                return null;

            ushort speed = BinaryPrimitives.ReadUInt16LittleEndian(device.Slice(wordOffset));
            if (speed == 0)
                return null; // unknown
            if (speed != 0xFFFF)
                return speed;

            // 0xFFFF means the speed is in the DWORD Extended field (SMBIOS 3.3+), whose bit 31 is reserved.
            if (device.Length < extendedOffset + 4)
                return null;
            uint extendedSpeed = BinaryPrimitives.ReadUInt32LittleEndian(device.Slice(extendedOffset)) & 0x7FFFFFFF;
            return extendedSpeed == 0 ? null : extendedSpeed;
        }

        private static string? MapSmbiosMemoryType(byte type) => type switch
        {
            0x12 => "DDR",
            0x13 => "DDR2",
            0x18 => "DDR3",
            0x1A => "DDR4",
            0x1B => "LPDDR",
            0x1C => "LPDDR2",
            0x1D => "LPDDR3",
            0x1E => "LPDDR4",
            0x20 => "HBM",
            0x21 => "HBM2",
            0x22 => "DDR5",
            0x23 => "LPDDR5",
            0x24 => "HBM3",
            0x25 => "MRDIMM",
            0x26 => "LPDDR6",
            _ => null
        };

        private static PhysicalMemoryInfo? GetMacMemory()
        {
            long total = 0;
            long? available = null;

            // 1. Get Total Memory
            string? sysctlOutput = ProcessHelper.RunAndReadOutput("sysctl", "-n hw.memsize");
            if (string.IsNullOrEmpty(sysctlOutput) || !long.TryParse(sysctlOutput!.Trim(), out total) || total == 0)
                return null;

            // 2. Get Available Memory using vm_stat
            string? vmStatOutput = ProcessHelper.RunAndReadOutput("vm_stat");
            if (!string.IsNullOrEmpty(vmStatOutput))
                available = ParseMacAvailableMemory(vmStatOutput!);

            var (speed, type) = GetMacMemoryDetails();
            return new PhysicalMemoryInfo(total, available, speed, type);
        }

        private static (long? SpeedMts, string? MemoryType) GetMacMemoryDetails()
        {
            try
            {
                string? json = ProcessHelper.RunAndReadOutput("system_profiler", "-json SPMemoryDataType", timeout: SystemProfilerTimeout);
                if (!string.IsNullOrEmpty(json))
                    return ParseMacMemoryJson(json!);
            }
            catch
            {
                // Fallback on parsing error
            }

            return (null, null);
        }

        /// <summary>
        /// Parses the output of <c>system_profiler -json SPMemoryDataType</c>.
        /// Apple silicon reports a single entry with <c>dimm_type</c> and no speed; Intel Macs report one <c>_items</c> entry per slot.
        /// </summary>
        internal static (long? SpeedMts, string? MemoryType) ParseMacMemoryJson(string json)
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("SPMemoryDataType", out var entries) || entries.ValueKind != JsonValueKind.Array)
                return (null, null);

            long? speed = null;
            string? memoryType = null;

            foreach (var entry in entries.EnumerateArray())
            {
                ReadMemoryDevice(entry);

                if (entry.TryGetProperty("_items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                        ReadMemoryDevice(item);
                }
            }

            return (speed, memoryType);

            void ReadMemoryDevice(JsonElement device)
            {
                if (device.ValueKind != JsonValueKind.Object)
                    return;

                // Empty slots report "empty" and virtual machines report placeholders such as "Unknown" or "RAM" (with a made-up speed),
                // so only devices with a real memory type are used.
                if (!device.TryGetProperty("dimm_type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String
                    || typeElement.GetString() is not string type || !Regex.IsMatch(type, "DDR|HBM"))
                {
                    return;
                }

                memoryType ??= type;

                // Despite the "MHz" label, the value is the transfer rate: DDR4-2667 is reported as "2667 MHz".
                if (device.TryGetProperty("dimm_speed", out var speedElement) && speedElement.ValueKind == JsonValueKind.String)
                {
                    var match = Regex.Match(speedElement.GetString() ?? "", @"^\s*(\d+)\s*MHz");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out long deviceSpeed) && deviceSpeed > 0
                        && (speed == null || deviceSpeed < speed.Value))
                    {
                        speed = deviceSpeed;
                    }
                }
            }
        }

        /// <summary>
        /// Available memory is free + speculative + inactive pages, the pages macOS can reuse for new allocations.
        /// vm_stat reports speculative pages separately from "Pages free". Only "Pages free" is required.
        /// </summary>
        internal static long? ParseMacAvailableMemory(string output)
        {
            long pageSize = 4096;
            var pageSizeMatch = Regex.Match(output, @"page size of (\d+) bytes");
            if (pageSizeMatch.Success && long.TryParse(pageSizeMatch.Groups[1].Value, out long parsedPageSize))
                pageSize = parsedPageSize;

            if (TryGetPages("free") is not long availablePages)
                return null;

            availablePages += TryGetPages("speculative") ?? 0;
            availablePages += TryGetPages("inactive") ?? 0;
            return availablePages * pageSize;

            long? TryGetPages(string pageName)
            {
                var match = Regex.Match(output, $@"Pages {pageName}:\s+(\d+)");
                return match.Success && long.TryParse(match.Groups[1].Value, out long pages) ? pages : null;
            }
        }
    }
}
