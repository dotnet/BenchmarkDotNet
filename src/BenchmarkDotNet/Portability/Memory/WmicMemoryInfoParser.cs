using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Memory
{
    internal static class WmicMemoryInfoParser
    {
        [CanBeNull]
        internal static MemoryInfo ParseOutput([CanBeNull] string content)
        {
            var memoryInfoContent = SectionsHelper.ParseSections(content, '=');
            long TotalMemory = 0L;
            long FreePhysicalMemory = 0L;
            bool isValid = memoryInfoContent.Count > 0;

            foreach (var memory in memoryInfoContent)
            {
                if (memory.TryGetValue(WmicMemoryInfoKeyNames.TotalVisibleMemorySize, out string totalMemoryValue) &&
                    long.TryParse(totalMemoryValue, out long totalMemory))
                {
                    TotalMemory += totalMemory;
                }
                else
                {
                    isValid = false;
                }

                if (memory.TryGetValue(WmicMemoryInfoKeyNames.FreePhysicalMemory, out string freePhysicalMemoryValue) &&
                    long.TryParse(freePhysicalMemoryValue, out long freePhysicalMemory))
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