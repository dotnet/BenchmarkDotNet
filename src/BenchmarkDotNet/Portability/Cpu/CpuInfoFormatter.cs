using System.Collections.Generic;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Portability.Cpu;

public static class CpuInfoFormatter
{
    public static string Format(CpuInfo? cpuInfo)
    {
        if (cpuInfo == null)
            return "Unknown processor";

        var parts = new List<string>
        {
            ProcessorBrandStringHelper.Prettify(cpuInfo, includeMaxFrequency: true)
        };

        if (cpuInfo.PhysicalProcessorCount > 0)
            parts.Add($", {cpuInfo.PhysicalProcessorCount} CPU");

        switch (cpuInfo.LogicalCoreCount)
        {
            case 1:
                parts.Add(", 1 logical core");
                break;
            case > 1:
                parts.Add($", {cpuInfo.LogicalCoreCount} logical cores");
                break;
        }

        if (cpuInfo.LogicalCoreCount > 0 && cpuInfo.PhysicalCoreCount > 0)
            parts.Add(" and ");
        else if (cpuInfo.PhysicalCoreCount > 0)
            parts.Add(", ");

        switch (cpuInfo.PhysicalCoreCount)
        {
            case 1:
                parts.Add("1 physical core");
                break;
            case > 1:
                parts.Add($"{cpuInfo.PhysicalCoreCount} physical cores");
                break;
        }

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