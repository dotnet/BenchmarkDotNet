using System.Collections.Generic;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Portability.Cpu
{
    public static class CpuInfoFormatter
    {
        public static string Format(CpuInfo cpuInfo)
            => Format(cpuInfo?.ProcessorName, cpuInfo?.PhysicalProcessorCount, cpuInfo?.PhysicalCoreCount, cpuInfo?.LogicalCoreCount, cpuInfo?.NominalFrequency);

        private static string Format(string processorName, int? physicalProcessorCount, int? physicalCoreCount, int? logicalCoreCount, double? processorFreq)
        {
            var parts = new List<string>
            {
                !string.IsNullOrWhiteSpace(processorName)
                    ? ProcessorBrandStringHelper.Prettify(processorName, processorFreq)
                    : "Unknown processor"
            };

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
            
            string result = string.Join("", parts);
            // The line with ProcessorBrandString is one of the longest lines in the summary.
            // When people past in on GitHub, it can be a reason of an ugly horizontal scrollbar.
            // To avoid this, we are trying to minimize this line and use the minimum possible number of characters.
            // Here we are removing the repetitive "cores" word.
            if (result.Contains("logical cores") && result.Contains("physical cores"))
                result = result.Replace("logical cores", "logical");

            return result;
        }
    }
}