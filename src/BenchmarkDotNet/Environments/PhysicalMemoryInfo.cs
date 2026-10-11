using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using System;
using System.Diagnostics;
using System.IO;
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
        public byte? CasLatency { get; }

        public PhysicalMemoryInfo(long totalPhysicalBytes, long? availablePhysicalBytes = null, long? frequencyMts = null, string? memoryType = null, byte? casLatency = null)
        {
            TotalPhysicalBytes = totalPhysicalBytes;
            AvailablePhysicalBytes = availablePhysicalBytes;
            FrequencyMts = frequencyMts;
            MemoryType = memoryType;
            CasLatency = casLatency;
        }

        public string ToFormattedString()
        {
            double totalGb = TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            var sb = new StringBuilder();
            sb.Append($"{Math.Round(totalGb, 2)} GB");

            if (MemoryType.IsNotBlank() || FrequencyMts.HasValue || CasLatency.HasValue)
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
                if (CasLatency.HasValue)
                {
                    if (MemoryType.IsNotBlank() || FrequencyMts.HasValue)
                        sb.Append('-');
                    sb.Append($"CAS{CasLatency}");
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
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (PInvoke.GlobalMemoryStatusEx(ref memStatus))
            {
                var (freq, type, cas) = GetLinuxOrWindowsMemoryDetails();
                return new PhysicalMemoryInfo((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys, freq, type, cas);
            }

            return null;
        }

        private static PhysicalMemoryInfo? GetLinuxMemory()
        {
            const string path = "/proc/meminfo";
            if (File.Exists(path))
            {
                long total = 0;
                long? available = null;

                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var match = Regex.Match(line, @"\d+");
                        if (match.Success && long.TryParse(match.Value, out long kb))
                            total = kb * 1024;
                    }
                    else if (line.StartsWith("MemAvailable:") || line.StartsWith("MemFree:"))
                    {
                        var match = Regex.Match(line, @"\d+");
                        if (match.Success && long.TryParse(match.Value, out long kb) && available == null)
                            available = kb * 1024;
                    }
                }

                if (total > 0)
                {
                    var (freq, type, cas) = GetLinuxOrWindowsMemoryDetails();
                    return new PhysicalMemoryInfo(total, available, freq, type, cas);
                }
            }
            return null;
        }

        private static (long? FrequencyMts, string? MemoryType, byte? CasLatency) GetLinuxOrWindowsMemoryDetails()
        {
            try
            {
                if (OsDetector.IsWindows7OrLater())
                {
                    return GetWindowsSmbiosMemoryDetails();
                }

                // Try reading /sys/devices/system/edac/mc/ on Linux
                if (Directory.Exists("/sys/devices/system/edac/mc"))
                {
                    foreach (var mcDir in Directory.GetDirectories("/sys/devices/system/edac/mc", "mc*"))
                    {
                        foreach (var dimmDir in Directory.GetDirectories(mcDir, "dimm*"))
                        {
                            string typePath = Path.Combine(dimmDir, "dimm_mem_type");
                            if (File.Exists(typePath))
                            {
                                string memType = File.ReadAllText(typePath).Trim();
                                if (!string.IsNullOrEmpty(memType))
                                    return (null, memType, null);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore fallback failures
            }

            return (null, null, null);
        }

        [SupportedOSPlatform("windows6.0.6000")]
        private static unsafe (long? FrequencyMts, string? MemoryType, byte? CasLatency) GetWindowsSmbiosMemoryDetails()
        {
            try
            {
                const uint rsMb = 0x52534D42; // 'RSMB'
                var provider = (FIRMWARE_TABLE_PROVIDER)rsMb;
                uint bufferSize = PInvoke.GetSystemFirmwareTable(provider, 0, null, 0);
                if (bufferSize == 0)
                    return (null, null, null);

                byte[] buffer = new byte[bufferSize];
                fixed (byte* pBuffer = buffer)
                {
                    if (PInvoke.GetSystemFirmwareTable(provider, 0, pBuffer, bufferSize) == 0)
                        return (null, null, null);
                }

                // SMBIOS table header: Data at offset 8
                int offset = 8;
                long? maxSpeed = null;
                string? detectedType = null;
                byte? casLatency = null;

                while (offset + 4 <= buffer.Length)
                {
                    byte type = buffer[offset];
                    byte length = buffer[offset + 1];

                    if (length < 4 || offset + length > buffer.Length)
                        break;

                    // Type 17: Memory Device
                    if (type == 17 && length >= 0x15)
                    {
                        // Memory Type at offset 0x12
                        byte rawMemoryType = buffer[offset + 0x12];
                        string? memTypeStr = MapSmbiosMemoryType(rawMemoryType);
                        if (memTypeStr != null && detectedType == null)
                        {
                            detectedType = memTypeStr;
                        }

                        // Speed in MHz at offset 0x15 (WORD), convert to MT/s
                        if (length >= 0x17)
                        {
                            ushort speedMhz = (ushort)(buffer[offset + 0x15] | (buffer[offset + 0x16] << 8));
                            if (speedMhz > 0 && speedMhz < 0xFFFF)
                            {
                                long speedMts = speedMhz * 2; // DDR: MT/s = MHz × 2
                                if (maxSpeed == null || speedMts > maxSpeed.Value)
                                {
                                    maxSpeed = speedMts;
                                }
                            }
                        }

                        // CAS Latency at offset 0x1C (BYTE)
                        if (length >= 0x1D && casLatency == null)
                        {
                            byte cas = buffer[offset + 0x1C];
                            if (cas > 0 && cas < 100) // Reasonable CAS latency range
                            {
                                casLatency = cas;
                            }
                        }
                    }

                    // Move past formatted area
                    offset += length;

                    // Skip unformatted string section (double null-terminated)
                    while (offset < buffer.Length - 1 && !(buffer[offset] == 0 && buffer[offset + 1] == 0))
                    {
                        offset++;
                    }
                    offset += 2;
                }

                return (maxSpeed, detectedType, casLatency);
            }
            catch
            {
                return (null, null, null);
            }
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

            // 2. Get Free Memory using vm_stat
            string? vmStatOutput = ProcessHelper.RunAndReadOutput("vm_stat");
            if (!string.IsNullOrEmpty(vmStatOutput))
            {
                long pageSize = 4096;
                var pageSizeMatch = Regex.Match(vmStatOutput, @"page size of (\d+) bytes");
                if (pageSizeMatch.Success && long.TryParse(pageSizeMatch.Groups[1].Value, out long parsedPageSize))
                {
                    pageSize = parsedPageSize;
                }

                var match = Regex.Match(vmStatOutput, @"Pages free:\s+(\d+)");
                if (match.Success && long.TryParse(match.Groups[1].Value, out long pagesFree))
                {
                    available = pagesFree * pageSize;
                }
            }

            var (freq, type) = GetMacMemorySpecs();
            return new PhysicalMemoryInfo(total, available, freq, type, null);
        }

        private static (long? Frequency, string? Type) GetMacMemorySpecs()
        {
            try
            {
                string? jsonOutput = ProcessHelper.RunAndReadOutput("system_profiler", "-json SPMemoryDataType");
                if (string.IsNullOrEmpty(jsonOutput))
                    return (null, null);

                using (JsonDocument doc = JsonDocument.Parse(jsonOutput!))
                {
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("SPMemoryDataType", out JsonElement memoryArray) || memoryArray.ValueKind != JsonValueKind.Array)
                        return (null, null);

                    long? maxFreq = null;
                    string? detectedType = null;

                    foreach (var memoryItem in memoryArray.EnumerateArray())
                    {
                        // Try to find Speed property (can be in different formats)
                        if (memoryItem.TryGetProperty("Speed", out JsonElement speedDirect) && speedDirect.ValueKind == JsonValueKind.String)
                        {
                            ParseSpeedValue(speedDirect.GetString(), ref maxFreq);
                        }

                        // Try _properties array structure
                        if (memoryItem.TryGetProperty("_properties", out JsonElement properties) && properties.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var prop in properties.EnumerateArray())
                            {
                                if (prop.TryGetProperty("Speed", out JsonElement speedProp) && speedProp.ValueKind == JsonValueKind.String)
                                {
                                    ParseSpeedValue(speedProp.GetString(), ref maxFreq);
                                }

                                if (prop.TryGetProperty("Type", out JsonElement typeProp) && typeProp.ValueKind == JsonValueKind.String)
                                {
                                    ParseTypeValue(typeProp.GetString(), ref detectedType);
                                }
                            }
                        }

                        // Try Type property (can be at top level)
                        if (memoryItem.TryGetProperty("Type", out JsonElement typeDirect) && typeDirect.ValueKind == JsonValueKind.String)
                        {
                            ParseTypeValue(typeDirect.GetString(), ref detectedType);
                        }
                    }

                    return (maxFreq, detectedType);
                }
            }
            catch
            {
                // Fallback on parsing error
            }

            return (null, null);
        }

        private static void ParseSpeedValue(string? speedStr, ref long? maxFreq)
        {
            if (string.IsNullOrEmpty(speedStr))
                return;

            var speedMatch = Regex.Match(speedStr, @"(\d+)\s*MHz");
            if (speedMatch.Success && long.TryParse(speedMatch.Groups[1].Value, out long speedMhz) && speedMhz > 0)
            {
                long speedMts = speedMhz * 2; // DDR: MT/s = MHz × 2
                if (maxFreq == null || speedMts > maxFreq.Value)
                    maxFreq = speedMts;
            }
        }

        private static void ParseTypeValue(string? typeStr, ref string? detectedType)
        {
            if (string.IsNullOrEmpty(typeStr) || typeStr == "Unknown")
                return;

            if (detectedType == null)
                detectedType = typeStr;
        }
    }
}