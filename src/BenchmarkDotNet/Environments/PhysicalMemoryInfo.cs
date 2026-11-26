using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Smbios;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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

    internal static class SystemMemory
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
                var (speed, type) = GetSmbiosMemoryDeviceDetails();
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
            // SmbiosDataAccessor returns no data when the files cannot be read.
            var details = GetSmbiosMemoryDeviceDetails();
            if (details.SpeedMts.HasValue || details.MemoryType != null)
                return details;

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

        private static (long? SpeedMts, string? MemoryType) GetSmbiosMemoryDeviceDetails()
        {
            if (!SmbiosDataAccessor.TryGetSmbiosData(out _, out var table))
                return (null, null);

            return ParseSmbiosMemoryDevices(table);
        }

        /// <summary>
        /// Reads the memory type and speed from the SMBIOS Memory Device (Type 17) structures.
        /// Returns <c>(null, null)</c> when SMBIOS is not available.
        /// </summary>
        internal static (long? SpeedMts, string? MemoryType) ParseSmbiosMemoryDevices(ReadOnlySpan<byte> table)
        {
            try
            {
                long? speed = null;
                string? memoryType = null;

                var devices = SmbiosDataParser.ParseMemoryDevicesInfo(table);

                foreach (SmbiosMemoryDeviceInfo device in devices)
                {
                    // 0x00 (Reserved) and 0x02 (Unknown) should not be shown as display name.
                    if (memoryType == null && device.MemoryTypeId is not (0x00 or 0x02))
                        memoryType = device.MemoryTypeName;

                    // When multiple memory modules exists, Use the speed of the slowest one.
                    long? deviceSpeed = GetMemoryDeviceSpeed(device);
                    if (deviceSpeed.HasValue && (speed == null || deviceSpeed.Value < speed.Value))
                        speed = deviceSpeed;
                }

                return (speed, memoryType);
            }
            catch
            {
                // SMBIOS is optional information; ignore failures and let the caller fall back.
                return (null, null);
            }
        }

        // Configured Memory Speed is what the module actually runs at.
        // Speed is the module's maximum rated speed, used only when the configured speed is not reported.
        private static long? GetMemoryDeviceSpeed(SmbiosMemoryDeviceInfo device)
        {
            uint configuredSpeed = device.ResolvedConfiguredSpeedMTs;
            if (configuredSpeed != 0)
                return configuredSpeed;

            uint ratedSpeed = device.ResolvedSpeedMTs;
            if (ratedSpeed != 0)
                return ratedSpeed;

            return null;
        }

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
