using System;
using System.Collections.Generic;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;

namespace BenchmarkDotNet.Helpers
{
    public static class ExternalToolsHelper
    {
        /// <summary>
        /// Output of the `wmic cpu list full` command.
        /// Windows only.
        /// </summary>
        public static readonly Lazy<WmicCpuInfoParser> Wmic = LazyParse(RuntimeInformation.IsWindows, "wmic",
            "cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List", s => new WmicCpuInfoParser(s));

        /// <summary>
        /// Output of the `cat /proc/info` command.
        /// Linux only.
        /// </summary>
        public static readonly Lazy<ProcCpuInfoParser> ProcCpuInfo = LazyParse(RuntimeInformation.IsLinux, "cat", "/proc/cpuinfo",
            s => new ProcCpuInfoParser(s));

        /// <summary>
        /// Output of the `lsb_release -a` command.
        /// Linux only.
        /// </summary>
        public static readonly Lazy<Dictionary<string, string>> LsbRelease = LazyParse(RuntimeInformation.IsLinux, "lsb_release", "-a",
            s => StringHelper.Parse(s, ':'));

        /// <summary>
        /// Output of the `sysctl -a` command.
        /// MacOSX only.
        /// </summary>
        public static readonly Lazy<SysctlCpuInfoParser> Sysctl = LazyParse(RuntimeInformation.IsMacOSX, "sysctl", "-a", s => new SysctlCpuInfoParser(s));

        /// <summary>
        /// Output of the `system_profiler SPSoftwareDataType` command.
        /// MacOSX only.
        /// </summary>
        public static readonly Lazy<Dictionary<string, string>> MacSystemProfilerData =
            LazyParse(RuntimeInformation.IsMacOSX, "system_profiler", "SPSoftwareDataType", s => StringHelper.Parse(s, ':'));

        private static Lazy<T> LazyParse<T>(Func<bool> isAvailable, string fileName, string arguments, Func<string, T> parseFunc)
        {
            return new Lazy<T>(() =>
            {
                string content = isAvailable() ? ProcessHelper.RunAndReadOutput(fileName, arguments) : "";
                return parseFunc(content);
            });
        }


    }
}