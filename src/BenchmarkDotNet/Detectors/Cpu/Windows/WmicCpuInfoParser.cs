using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal static class WmicCpuInfoParser
{
    /// <summary>
    /// Parses wmic output and returns <see cref="CpuInfo"/>
    /// </summary>
    /// <param name="wmicOutput">Output of `wmic cpu get Name, NumberOfCores, NumberOfLogicalProcessors /Format:List`</param>
    internal static CpuInfo Parse(string wmicOutput)
    {
        HashSet<string> processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorsCount = 0;
        double maxFrequency = 0.0;
        double nominalFrequency = 0.0;

        List<Dictionary<string, string>> processors = SectionsHelper.ParseSections(wmicOutput, '=');
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
                nominalFrequency = nominalFrequency == 0 ? frequency : Math.Min(nominalFrequency, frequency);
                maxFrequency = Math.Max(maxFrequency, frequency);
            }
        }

        string? processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        Frequency? maxFrequencyActual = maxFrequency > 0 && processorsCount > 0
            ? Frequency.FromMHz(maxFrequency)
            : null;

        Frequency? nominalFrequencyActual = nominalFrequency > 0 && processorsCount > 0 ? Frequency.FromMHz(nominalFrequency) : null;

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : null,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong()
        };
    }
}