using BenchmarkDotNet.Helpers;
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

                if (memory.TryGetValue(ProcMemoryInfoKeyNames.MemAvailable, out string availablePhysicalMemoryValue) &&
                    long.TryParse(availablePhysicalMemoryValue.Replace("kB", string.Empty), out long availablePhysicalMemory))
                {
                    FreePhysicalMemory += availablePhysicalMemory;
                }
                else
                {
                    // Fallback for kernels < 3.14 where proc/meminfo does not provide "MemAvailable"
                    // calculated as Free + cached
                    if (memory.TryGetValue(ProcMemoryInfoKeyNames.MemFree, out string freePhysicalMemoryValue) &&
                       long.TryParse(freePhysicalMemoryValue.Replace("kB", string.Empty), out long freePhysicalMemory) &&
                       memory.TryGetValue(ProcMemoryInfoKeyNames.Cached, out string cachedMemoryValue) &&
                       long.TryParse(cachedMemoryValue.Replace("kB", string.Empty), out long cachedMemory))
                    {
                        FreePhysicalMemory += (freePhysicalMemory + cachedMemory);
                    }
                    else
                    {
                        isValid = false;
                    }
                }
            }

            var memoryInfo = isValid ? new MemoryInfo(TotalMemory, FreePhysicalMemory) : null;
            return memoryInfo;
        }
    }
}