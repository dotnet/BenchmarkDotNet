using System;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `cat /proc/info` command.
    /// Linux only.
    /// </summary>
    internal static class ProcCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> ProcCpuInfo = new Lazy<CpuInfo>(Load);

        [CanBeNull]
        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsLinux())
            {
                string content = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo");
                return ProcCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}