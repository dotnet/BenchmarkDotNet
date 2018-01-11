using System;
using System.Collections.Generic;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;

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
        public static readonly Lazy<Dictionary<string, string>> LsbRelease = LazyDic(RuntimeInformation.IsLinux, "lsb_release", "-a", ':');

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
            LazyDic(RuntimeInformation.IsMacOSX, "system_profiler", "SPSoftwareDataType", ':');

        private static Lazy<Dictionary<string, string>> LazyDic(Func<bool> isAvailable, string fileName, string arguments, char separator)
        {
            return new Lazy<Dictionary<string, string>>(() =>
            {
                string content = isAvailable() ? ProcessHelper.RunAndReadOutput(fileName, arguments) : "";
                return ParseList(content, separator);
            });
        }

        private static Lazy<T> LazyParse<T>(Func<bool> isAvailable, string fileName, string arguments, Func<string, T> parseFunc)
        {
            return new Lazy<T>(() =>
            {
                string content = isAvailable() ? ProcessHelper.RunAndReadOutput(fileName, arguments) : "";
                return parseFunc(content);
            });
        }

        [NotNull]
        private static Dictionary<string, string> ParseList([CanBeNull] string content, char separator)
        {
            var values = new Dictionary<string, string>();
            var list = content?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (list != null)
                foreach (string line in list)
                    if (line.IndexOf(separator) != -1)
                    {
                        var lineParts = line.Split(separator);
                        if (lineParts.Length >= 2)
                            values[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
            return values;
        }
    }
}