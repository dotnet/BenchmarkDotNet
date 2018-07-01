using System;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `wmic get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List` command.
    /// Windows only.
    /// </summary>
    internal static class WmicCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> WmicCpuInfo = new Lazy<CpuInfo>(Load);

        [CanBeNull]
        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsWindows())
            {
                string content = ProcessHelper.RunAndReadOutput("wmic", "cpu get Name, NumberOfCores, NumberOfLogicalProcessors, CurrentClockSpeed /Format:List");
                return WmicCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}