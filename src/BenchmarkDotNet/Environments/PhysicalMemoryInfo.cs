using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace BenchmarkDotNet.Environments
{
    public class PhysicalMemoryInfo
    {
        public long TotalPhysicalBytes { get; }
        public long? AvailablePhysicalBytes { get; }
        public long? FrequencyMHz { get; }
        public string? MemoryType { get; }

        public PhysicalMemoryInfo(long totalPhysicalBytes, long? availablePhysicalBytes = null, long? frequencyMHz = null, string? memoryType = null)
        {
            TotalPhysicalBytes = totalPhysicalBytes;
            AvailablePhysicalBytes = availablePhysicalBytes;
            FrequencyMHz = frequencyMHz;
            MemoryType = memoryType;
        }

        public string ToFormattedString()
        {
            double totalGb = TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            var sb = new StringBuilder();
            sb.Append($"{Math.Round(totalGb, 2)} GB");

            if (MemoryType.IsNotBlank() || FrequencyMHz.HasValue)
            {
                sb.Append(" (");
                if (MemoryType.IsNotBlank())
                    sb.Append(MemoryType);
                if (FrequencyMHz.HasValue)
                {
                    if (MemoryType.IsNotBlank())
                        sb.Append('-');
                    sb.Append($"{FrequencyMHz}MHz");
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
                var (freq, type) = GetLinuxOrWindowsMemoryDetails();
                return new PhysicalMemoryInfo((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys, freq, type);
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
                    var (freq, type) = GetLinuxOrWindowsMemoryDetails();
                    return new PhysicalMemoryInfo(total, available, freq, type);
                }
            }
            return null;
        }

        private static (long? FrequencyMHz, string? MemoryType) GetLinuxOrWindowsMemoryDetails()
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
                                if (!string.IsNullOrEmpty(memType) && memType != "Unspecified")
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

        [SupportedOSPlatform("windows6.0.6000")]
        private static unsafe (long? FrequencyMHz, string? MemoryType) GetWindowsSmbiosMemoryDetails()
        {
            try
            {
                const uint rsMb = 0x52534D42; // 'RSMB'
                var provider = (FIRMWARE_TABLE_PROVIDER)rsMb;
                uint bufferSize = PInvoke.GetSystemFirmwareTable(provider, 0, null, 0);
                if (bufferSize == 0)
                    return (null, null);

                byte[] buffer = new byte[bufferSize];
                fixed (byte* pBuffer = buffer)
                {
                    if (PInvoke.GetSystemFirmwareTable(provider, 0, pBuffer, bufferSize) == 0)
                        return (null, null);
                }

                // SMBIOS table header: Data at offset 8
                int offset = 8;
                long? maxSpeed = null;
                string? detectedType = null;

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

                        // Speed in MHz at offset 0x15 (WORD)
                        if (length >= 0x17)
                        {
                            ushort speed = (ushort)(buffer[offset + 0x15] | (buffer[offset + 0x16] << 8));
                            if (speed > 0 && speed < 0xFFFF)
                            {
                                if (maxSpeed == null || speed > maxSpeed.Value)
                                {
                                    maxSpeed = speed;
                                }
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

                return (maxSpeed, detectedType);
            }
            catch
            {
                return (null, null);
            }
        }

        private static string? MapSmbiosMemoryType(byte type) => type switch
        {
            0x18 => "DDR3",
            0x1A => "DDR4",
            0x1B => "LPDDR",
            0x1C => "LPDDR2",
            0x1D => "LPDDR3",
            0x1E => "LPDDR4",
            0x22 => "DDR5",
            0x23 => "LPDDR5",
            0x25 => "MRDIMM",
            0x26 => "LPDDR6",
            _ => null
        };

        private static PhysicalMemoryInfo? GetMacMemory()
        {
            long total = 0;
            long? available = null;

            // 1. Get Total Memory
            var sysctlInfo = new ProcessStartInfo("sysctl", "-n hw.memsize")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(sysctlInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    long.TryParse(output.Trim(), out total);
                }
            }

            if (total == 0) return null;

            // 2. Get Free Memory using vm_stat
            var vmStatInfo = new ProcessStartInfo("vm_stat")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(vmStatInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    long pageSize = 4096;
                    var pageSizeMatch = Regex.Match(output, @"page size of (\d+) bytes");
                    if (pageSizeMatch.Success && long.TryParse(pageSizeMatch.Groups[1].Value, out long parsedPageSize))
                    {
                        pageSize = parsedPageSize;
                    }

                    var match = Regex.Match(output, @"Pages free:\s+(\d+)");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out long pagesFree))
                    {
                        available = pagesFree * pageSize;
                    }
                }
            }

            return new PhysicalMemoryInfo(total, available);
        }
    }
}