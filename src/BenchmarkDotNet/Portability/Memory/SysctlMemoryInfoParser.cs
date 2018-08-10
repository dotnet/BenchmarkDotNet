using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;
#if !NETCOREAPP2_1
using BenchmarkDotNet.Extensions;
#endif

namespace BenchmarkDotNet.Portability.Memory
{
    internal static class SysctlMemoryInfoParser
    {
        [CanBeNull]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static MemoryInfo ParseOutput([CanBeNull] string systlContent, [CanBeNull] string vmStatContent)
        {
            var sysctl = SectionsHelper.ParseSection(systlContent, ':');
            var vmstat = SectionsHelper.ParseSection(vmStatContent, ':');
            long? TotalMemory = null;
            long? FreePhysicalMemory = null;

            if (sysctl.TryGetValue("hw.memsize", out string totalMemoryValue) && long.TryParse(totalMemoryValue, out long totalMemory))
            {
                // hw.memsize returns in bytes
                TotalMemory = (long)(totalMemory/1024.0);
            }
            if (vmstat.TryGetValue("Pages free", out string freeMemoryValue) && long.TryParse(freeMemoryValue.Replace(".",string.Empty), out long freeMemory))
            {
                // Pages are in 4K units
                FreePhysicalMemory = freeMemory * 4;
            }

            if (TotalMemory.HasValue && FreePhysicalMemory.HasValue)
            {
                return new MemoryInfo(TotalMemory.Value, FreePhysicalMemory.Value);
            }
            else
            {
                return null;
            }
        }     
    }
}