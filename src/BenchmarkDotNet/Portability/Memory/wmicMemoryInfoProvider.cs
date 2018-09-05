using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Portability.Memory
{
    /// <summary>
    /// Memory information from output of the `wmic OS get TotalVisibleMemorySize, FreePhysicalMemory /Format:List` command.
    /// Windows only.
    /// </summary>
    internal static class WmicMemoryInfoProvider
    {
        internal static readonly Lazy<MemoryInfo> WmicMemoryInfo = new Lazy<MemoryInfo>(Load);

        [CanBeNull]
        private static MemoryInfo Load()
        {
            if (RuntimeInformation.IsWindows())
            {
                string content = ProcessHelper.RunAndReadOutput("wmic", "OS get TotalVisibleMemorySize, FreePhysicalMemory /Format:List");
                return WmicMemoryInfoParser.ParseOutput(content);
            }
            return null;
        }
    }
}
