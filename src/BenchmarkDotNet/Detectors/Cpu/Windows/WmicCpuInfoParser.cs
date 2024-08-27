using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal static class WmicCpuInfoParser
{
    /// <summary>
    /// Parses wmic output and returns <see cref="PhdCpu"/>
    /// </summary>
    /// <param name="wmicOutput">Output of `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List`</param>
    internal static PhdCpu Parse(string? wmicOutput)
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

        return new PhdCpu
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : null,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = maxFrequency?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequency?.Hertz.RoundToLong()
        };
    }
}