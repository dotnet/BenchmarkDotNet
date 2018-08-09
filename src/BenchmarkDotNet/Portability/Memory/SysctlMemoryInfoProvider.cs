using System;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Memory
{
    /// <summary>    
    /// Memory information from output of the `sysctl -a` command.
    /// MacOSX only.
    /// </summary>
    internal static class SysctlMemoryInfoProvider
    {
        internal static readonly Lazy<MemoryInfo> SysctlMemoryInfo = new Lazy<MemoryInfo>(Load);

        [CanBeNull]
        private static MemoryInfo Load()
        {
            if (RuntimeInformation.IsMacOSX())
            {
                string sysctlContent = ProcessHelper.RunAndReadOutput("sysctl", "-a");
                string vmsStatContent = ProcessHelper.RunAndReadOutput("vm_stat");
                return SysctlMemoryInfoParser.ParseOutput(sysctlContent, vmsStatContent);
            }
            return null;
        }
    }
}