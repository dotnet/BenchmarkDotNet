using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

internal static class ProcCpuParser
{
    internal static PhdCpu Parse(string? content)
    {
        var logicalCores = SectionsHelper.ParseSections(content, ':');
        var processorModelNames = new HashSet<string>();
        var processorsToPhysicalCoreCount = new Dictionary<string, int>();

        int logicalCoreCount = 0;
        var nominalFrequency = Frequency.Zero;
        var minFrequency = Frequency.Zero;
        var maxFrequency = Frequency.Zero;

        foreach (var logicalCore in logicalCores)
        {
            if (logicalCore.TryGetValue(ProcCpuKeyNames.PhysicalId, out string physicalId) &&
                logicalCore.TryGetValue(ProcCpuKeyNames.CpuCores, out string cpuCoresValue) &&
                int.TryParse(cpuCoresValue, out int cpuCoreCount) &&
                cpuCoreCount > 0)
                processorsToPhysicalCoreCount[physicalId] = cpuCoreCount;

            if (logicalCore.TryGetValue(ProcCpuKeyNames.ModelName, out string modelName))
            {
                processorModelNames.Add(modelName);
                nominalFrequency = ParseFrequencyFromBrandString(modelName);
                logicalCoreCount++;
            }

            if (logicalCore.TryGetValue(ProcCpuKeyNames.MinFrequency, out string minCpuFreqValue)
                && Frequency.TryParseMHz(minCpuFreqValue, out var minCpuFreq))
            {
                minFrequency = minCpuFreq;
            }

            if (logicalCore.TryGetValue(ProcCpuKeyNames.MaxFrequency, out string maxCpuFreqValue)
                && Frequency.TryParseMHz(maxCpuFreqValue, out var maxCpuFreq))
            {
                maxFrequency = maxCpuFreq;
            }
        }

        return new PhdCpu
        {
            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
            PhysicalProcessorCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Count : null,
            PhysicalCoreCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Values.Sum() : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequency > 0 ? nominalFrequency.Hertz.RoundToLong() : null,
            MinFrequencyHz = minFrequency > 0 ? minFrequency.Hertz.RoundToLong() : null,
            MaxFrequencyHz = maxFrequency > 0 ? maxFrequency.Hertz.RoundToLong() : null
        };
    }

    internal static Frequency ParseFrequencyFromBrandString(string brandString)
    {
        const string pattern = "(\\d.\\d+)GHz";
        var matches = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase);
        if (matches.Count > 0 && matches[0].Groups.Count > 1)
        {
            string match = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase)[0].Groups[1].ToString();
            return Frequency.TryParseGHz(match, out var result) ? result : Frequency.Zero;
        }

        return 0d;
    }
}