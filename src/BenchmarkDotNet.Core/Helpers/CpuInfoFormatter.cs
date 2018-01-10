using System.Collections.Generic;

namespace BenchmarkDotNet.Helpers
{
    public static class CpuInfoFormatter
    {
        public static string Format(ICpuInfo cpuInfo)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(cpuInfo.ProcessorName))
                parts.Add(cpuInfo.ProcessorName);
            else
                parts.Add("Unknown processor");
            
            if (cpuInfo.PhysicalProcessorCount > 0)
                parts.Add($", {cpuInfo.PhysicalProcessorCount} CPU");
            
            if (cpuInfo.LogicalCoreCount == 1)
                parts.Add(", 1 logical core");
            if (cpuInfo.LogicalCoreCount > 1)
                parts.Add($", {cpuInfo.LogicalCoreCount} logical cores");
            
            if (cpuInfo.LogicalCoreCount > 0 && cpuInfo.PhysicalCoreCount > 0)
                parts.Add(" and ");
            else if (cpuInfo.PhysicalCoreCount > 0)
                parts.Add(", ");
            
            if (cpuInfo.PhysicalCoreCount == 1)
                parts.Add("1 physical core");
            if (cpuInfo.PhysicalCoreCount > 1)
                parts.Add($"{cpuInfo.PhysicalCoreCount} physical cores");

            return string.Join("", parts);
        }
    }
}