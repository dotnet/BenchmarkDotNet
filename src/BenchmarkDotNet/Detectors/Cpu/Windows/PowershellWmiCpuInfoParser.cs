using System;
using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal static class PowershellWmiCpuInfoParser
{
    internal static CpuInfo? Parse(string? powershellWmiOutput)
    {
        if (string.IsNullOrEmpty(powershellWmiOutput))
            return CpuInfo.Unknown;

        HashSet<string> processorModelNames = new HashSet<string>();

        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorCount = 0;
        double maxFrequency = 0.0;
        double nominalFrequency = 0.0;

        List<Dictionary<string, string>> processors = SectionsHelper.ParseSections(powershellWmiOutput, ':');
        foreach (Dictionary<string, string> processor in processors)
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
                processorCount++;
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
        Frequency? maxFrequencyActual = maxFrequency > 0 && processorCount > 0
            ? Frequency.FromMHz(maxFrequency)
            : null;

        Frequency? nominalFrequencyActual = nominalFrequency > 0 && processorCount > 0 ?
            Frequency.FromMHz(nominalFrequency) : null;

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = processorCount > 0 ? processorCount : null,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong()
        };
    }
}