using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Environments
{
    public class PhysicalMemoryInfo
    {
        public long TotalPhysicalBytes { get; }

        public PhysicalMemoryInfo(long totalPhysicalBytes)
        {
            TotalPhysicalBytes = totalPhysicalBytes;
        }

        public string ToFormattedString()
        {
            double gb = TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            return $"{Math.Round(gb, 2)} GB";
        }
    }

    public static class SystemMemory
    {
        public static PhysicalMemoryInfo? GetPhysicalMemory()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetWindowsMemory();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return GetLinuxMemory();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return GetMacMemory();
            }
            catch (Exception)
            {

            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private static PhysicalMemoryInfo? GetWindowsMemory()
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                return new PhysicalMemoryInfo((long)memStatus.ullTotalPhys);
            }
            return null;
        }

        private static PhysicalMemoryInfo? GetLinuxMemory()
        {
            const string path = "/proc/meminfo";
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var match = Regex.Match(line, @"\d+");
                        if (match.Success && long.TryParse(match.Value, out long kb))
                        {
                            return new PhysicalMemoryInfo(kb * 1024);
                        }
                    }
                }
            }
            return null;
        }

        private static PhysicalMemoryInfo? GetMacMemory()
        {
            var info = new ProcessStartInfo("sysctl", "-n hw.memsize")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(info))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (long.TryParse(output.Trim(), out long bytes))
                    {
                        return new PhysicalMemoryInfo(bytes);
                    }
                }
            }
            return null;
        }
    }
}