using System;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>
    /// CPU information from output of the `sysctl -a` command.
    /// MacOSX only.
    /// </summary>
    internal static class SysctlCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> SysctlCpuInfo = new Lazy<CpuInfo>(Load);

        private static CpuInfo? Load()
        {
            if (RuntimeInformation.IsMacOS())
            {
                string content = ProcessHelper.RunAndReadOutput("sysctl", "-a");
                return SysctlCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}