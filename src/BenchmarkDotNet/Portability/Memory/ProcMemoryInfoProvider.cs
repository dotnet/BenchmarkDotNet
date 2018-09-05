using System;
using System.Linq;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Memory
{
    /// <summary>
    /// Memory information from output of the `cat /proc/meminfo` command.
    /// Linux only.
    /// </summary>
    internal static class ProcMemoryInfoProvider
    {
        internal static readonly Lazy<MemoryInfo> ProcMemoryInfo = new Lazy<MemoryInfo>(Load);

        [CanBeNull]
        private static MemoryInfo Load()
        {
            if (RuntimeInformation.IsLinux())
            {
                string content = ProcessHelper.RunAndReadOutput("cat", "/proc/meminfo");                
                return ProcMemoryInfoParser.ParseOutput(content);
            }
            return null;
        }        
    }
}