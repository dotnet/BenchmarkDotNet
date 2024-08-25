using System.Collections.Generic;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Portability.Cpu.Windows;

internal static class WmicCpuInfoParser
{
    /// <summary>
    /// Parses wmic output and returns <see cref="CpuInfo"/>
    /// </summary>
    /// <param name="wmicOutput">Output of `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List`</param>
    internal static CpuInfo Parse(string? wmicOutput)
    {
        var processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorsCount = 0;
        var sumMaxFrequency = Frequency.Zero;

        var processors = SectionsHelper.ParseSections(wmicOutput, '=');
        foreach (var processor in processors)
        {
            if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfCores, out string numberOfCoresValue) &&
                int.TryParse(numberOfCoresValue, out int numberOfCores) &&
                numberOfCores > 0)
                physicalCoreCount += numberOfCores;

            if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfLogicalProcessors, out string numberOfLogicalValue) &&
                int.TryParse(numberOfLogicalValue, out int numberOfLogical) &&
                numberOfLogical > 0)
                logicalCoreCount += numberOfLogical;

            if (processor.TryGetValue(WmicCpuInfoKeyNames.Name, out string name))
            {
                processorModelNames.Add(name);
                processorsCount++;
            }

            if (processor.TryGetValue(WmicCpuInfoKeyNames.MaxClockSpeed, out string frequencyValue)
                && int.TryParse(frequencyValue, out int frequency)
                && frequency > 0)
            {
                sumMaxFrequency += frequency;
            }
        }

        string? processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        Frequency? maxFrequency = sumMaxFrequency > 0 && processorsCount > 0
            ? Frequency.FromMHz(sumMaxFrequency / processorsCount)
            : null;

        return new CpuInfo(
            processorName,
            GetCount(processorsCount), GetCount(physicalCoreCount), GetCount(logicalCoreCount),
            maxFrequency, maxFrequency);

        int? GetCount(int count) => count > 0 ? count : null;
    }
}