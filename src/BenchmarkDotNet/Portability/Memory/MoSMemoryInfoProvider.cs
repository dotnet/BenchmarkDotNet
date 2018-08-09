using JetBrains.Annotations;
using System;
using System.Linq;
using System.Management;

namespace BenchmarkDotNet.Portability.Memory
{
    internal static class MoSMemoryInfoProvider
    {
        internal static readonly Lazy<MemoryInfo> MosMemoryInfo = new Lazy<MemoryInfo>(Load);

        [CanBeNull]
        private static MemoryInfo Load()
        {
            var mosMemory = new ManagementObjectSearcher("SELECT * FROM CIM_OperatingSystem");
            long TotalMemory = 0L;
            long FreePhysicalMemory = 0L;
            var memoryInfoContent = mosMemory.Get().Cast<ManagementObject>().ToList();
            bool isValid = memoryInfoContent.Count > 0;

            foreach (var memory in mosMemory.Get().Cast<ManagementObject>())
            {
                foreach (var prop in memory.Properties)
                {
                    if (string.Equals(prop.Name, WmicMemoryInfoKeyNames.TotalVisibleMemorySize, StringComparison.Ordinal))
                    {
                        TotalMemory += (long)(ulong)memory.GetPropertyValue(WmicMemoryInfoKeyNames.TotalVisibleMemorySize);
                    }
                    else if (string.Equals(prop.Name, WmicMemoryInfoKeyNames.FreePhysicalMemory, StringComparison.Ordinal))
                    {
                        FreePhysicalMemory += (long)(ulong)memory.GetPropertyValue(WmicMemoryInfoKeyNames.FreePhysicalMemory);
                    }
                }
            }

            var memoryInfo = isValid ? new MemoryInfo(TotalMemory, FreePhysicalMemory) : null;
            return memoryInfo;
        }
    }
}
