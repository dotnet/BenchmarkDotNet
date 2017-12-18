﻿using System;
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
        public static readonly Lazy<Dictionary<string, string>> Wmic = LazyDic(RuntimeInformation.IsWindows, "wmic", "cpu list full", '=');

        /// <summary>
        /// Output of the `cat /proc/info` command.
        /// Linux only.
        /// </summary>
        public static readonly Lazy<ProcCpuInfoParser> ProcCpuInfo = LazyProcCpuInfo(RuntimeInformation.IsLinux, "cat", "/proc/cpuinfo");

        /// <summary>
        /// Output of the `lsb_release -a` command.
        /// Linux only.
        /// </summary>
        public static readonly Lazy<Dictionary<string, string>> LsbRelease = LazyDic(RuntimeInformation.IsLinux, "lsb_release", "-a", ':');

        /// <summary>
        /// Output of the `sysctl -a` command.
        /// MacOSX only.
        /// </summary>
        public static readonly Lazy<Dictionary<string, string>> Sysctl = LazyDic(RuntimeInformation.IsMacOSX, "sysctl", "-a", ':');
        
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
       
        private static Lazy<ProcCpuInfoParser> LazyProcCpuInfo(Func<bool> isAvailable, string fileName, string arguments)
        {
            return new Lazy<ProcCpuInfoParser>(() =>
            {
                string content = isAvailable() ? ProcessHelper.RunAndReadOutput(fileName, arguments) : "";
                return new ProcCpuInfoParser(content);
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