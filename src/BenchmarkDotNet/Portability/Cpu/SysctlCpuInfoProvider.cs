using System;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    /// <summary>    
    /// CPU information from output of the `sysctl -a` command. 
    /// It is cached by SysctlInfoProvider for reuse in memory and CPU
    /// MacOSX only.
    /// </summary>
    internal static class SysctlCpuInfoProvider
    {
        internal static readonly Lazy<CpuInfo> SysctlCpuInfo = new Lazy<CpuInfo>(Load);

        [CanBeNull]
        private static CpuInfo Load()
        {
            if (RuntimeInformation.IsMacOSX())
            {
                string content = SysctlInfoProvider.SysctlInfo.Value;
                return SysctlCpuInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}