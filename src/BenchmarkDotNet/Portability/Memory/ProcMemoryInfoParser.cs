using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Memory
{
    internal static class ProcMemoryInfoParser
    {
        [CanBeNull]
        internal static MemoryInfo ParseOutput([CanBeNull] string content)
        {
            var memoryInfoContent = SectionsHelper.ParseSections(content, ':');
            long TotalMemory = 0L;
            long FreePhysicalMemory = 0L;
            bool isValid = memoryInfoContent.Count > 0;

            foreach (var memory in memoryInfoContent)
            {
                if (memory.TryGetValue(ProcMemoryInfoKeyNames.MemTotal, out string totalMemoryValue) &&
                    long.TryParse(totalMemoryValue.Replace("kB", string.Empty), out long totalMemory))
                {
                    TotalMemory += totalMemory;
                }
                else
                {
                    isValid = false;
                }

                if (memory.TryGetValue(ProcMemoryInfoKeyNames.MemFree, out string freePhysicalMemoryValue) &&
                    long.TryParse(freePhysicalMemoryValue.Replace("kB",string.Empty), out long freePhysicalMemory))
                {
                    FreePhysicalMemory += freePhysicalMemory;
                }
                else
                {
                    isValid = false;
                }
            }

            var memoryInfo = isValid ? new MemoryInfo(TotalMemory, FreePhysicalMemory) : null;
            return memoryInfo;
        }
    }
}