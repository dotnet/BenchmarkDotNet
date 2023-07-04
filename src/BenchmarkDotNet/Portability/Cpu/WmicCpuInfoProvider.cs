using System;
using System.IO;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
    /// Windows only.
    /// </summary>
    internal static class WmicCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> WmicCpuInfo = new (Load);

        private const string DefaultWmicPath = @"C:\Windows\System32\wbem\WMIC.exe";

        private static CpuInfo? Load()
        {
            if (RuntimeInformation.IsWindows())
            {
                const string argList = $"{WmicCpuInfoKeyNames.Name}, {WmicCpuInfoKeyNames.NumberOfCores}, " +
                                       $"{WmicCpuInfoKeyNames.NumberOfLogicalProcessors}, {WmicCpuInfoKeyNames.MaxClockSpeed}";
                string wmicPath = File.Exists(DefaultWmicPath) ? DefaultWmicPath : "wmic";
                string content = ProcessHelper.RunAndReadOutput(wmicPath, $"cpu get {argList} /Format:List");
                return WmicCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}