using System.Collections.Generic;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Portability.Cpu
{
    public static class CpuInfoFormatter
    {
        public static string Format(CpuInfo cpuInfo)
            => Format(cpuInfo?.ProcessorName, cpuInfo?.PhysicalProcessorCount, cpuInfo?.PhysicalCoreCount, cpuInfo?.LogicalCoreCount);

        private static string Format(string processorName, int? physicalProcessorCount, int? physicalCoreCount, int? logicalCoreCount)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(processorName))
                parts.Add(ProcessorBrandStringHelper.Prettify(processorName));
            else
                parts.Add("Unknown processor");

            if (physicalProcessorCount > 0)
                parts.Add($", {physicalProcessorCount} CPU");

            if (logicalCoreCount == 1)
                parts.Add(", 1 logical core");
            if (logicalCoreCount > 1)
                parts.Add($", {logicalCoreCount} logical cores");

            if (logicalCoreCount > 0 && physicalCoreCount > 0)
                parts.Add(" and ");
            else if (physicalCoreCount > 0)
                parts.Add(", ");

            if (physicalCoreCount == 1)
                parts.Add("1 physical core");
            if (physicalCoreCount > 1)
                parts.Add($"{physicalCoreCount} physical cores");

            return string.Join("", parts);
        }
    }
}