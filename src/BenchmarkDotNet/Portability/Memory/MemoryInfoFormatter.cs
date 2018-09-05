using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Portability.Memory
{
    public class MemoryInfoFormatter
    {
        public static string Format(MemoryInfo memoryInfo)
        {
            string totalMemory = $"Total Memory={Math.Round((memoryInfo.TotalMemory) / (1024.0 * 1024.0), 2)} GB";
            string freeMemory = $"Available Memory={Math.Round(memoryInfo.FreePhysicalMemory / (1024.0 * 1024.0), 2)} GB";

            return $"{totalMemory}, {freeMemory}";
        }
    }
}
