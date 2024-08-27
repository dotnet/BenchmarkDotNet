using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.Linux;

internal static class LinuxCpuInfoParser
{
    private static class ProcCpu
    {
        internal const string PhysicalId = "physical id";
        internal const string CpuCores = "cpu cores";
        internal const string ModelName = "model name";
        internal const string MaxFrequency = "max freq";
    }

    private static class Lscpu
    {
        internal const string MaxFrequency = "CPU max MHz";
        internal const string ModelName = "Model name";
        internal const string CoresPerSocket = "Core(s) per socket";
    }

    /// <param name="cpuInfo">Output of `cat /proc/cpuinfo`</param>
    /// <param name="lscpu">Output of `lscpu`</param>
    internal static PhdCpu Parse(string? cpuInfo, string? lscpu)
    {
        var processorModelNames = new HashSet<string>();
        var processorsToPhysicalCoreCount = new Dictionary<string, int>();
        int logicalCoreCount = 0;
        Frequency? maxFrequency = null;

        var logicalCores = SectionsHelper.ParseSections(cpuInfo, ':');
        foreach (var logicalCore in logicalCores)
        {
            if (logicalCore.TryGetValue(ProcCpu.PhysicalId, out string physicalId) &&
                logicalCore.TryGetValue(ProcCpu.CpuCores, out string cpuCoresValue) &&
                int.TryParse(cpuCoresValue, out int cpuCoreCount) &&
                cpuCoreCount > 0)
                processorsToPhysicalCoreCount[physicalId] = cpuCoreCount;

            if (logicalCore.TryGetValue(ProcCpu.ModelName, out string modelName))
            {
                processorModelNames.Add(modelName);
                logicalCoreCount++;
            }

            if (logicalCore.TryGetValue(ProcCpu.MaxFrequency, out string maxCpuFreqValue) &&
                Frequency.TryParseMHz(maxCpuFreqValue, out var maxCpuFreq))
            {
                maxFrequency = maxCpuFreq;
            }
        }

        int? coresPerSocket = null;
        if (lscpu != null)
        {
            var lscpuParts = lscpu.Split('\n')
                .Where(line => line.Contains(':'))
                .SelectMany(line => line.Split([':'], 2))
                .ToList();
            for (int i = 0; i + 1 < lscpuParts.Count; i += 2)
            {
                string name = lscpuParts[i].Trim();
                string value = lscpuParts[i + 1].Trim();

                if (name.EqualsWithIgnoreCase(Lscpu.MaxFrequency) &&
                    Frequency.TryParseMHz(value.Replace(',', '.'), out var maxFrequencyParsed)) // Example: `CPU max MHz: 3200,0000`
                    maxFrequency = maxFrequencyParsed;

                if (name.EqualsWithIgnoreCase(Lscpu.ModelName))
                    processorModelNames.Add(value);

                if (name.EqualsWithIgnoreCase(Lscpu.CoresPerSocket) &&
                    int.TryParse(value, out int coreCount))
                    coresPerSocket = coreCount;
            }
        }

        var nominalFrequency = processorModelNames
            .Select(ParseFrequencyFromBrandString)
            .WhereNotNull()
            .FirstOrDefault() ?? maxFrequency;
        string processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        int? physicalProcessorCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Count : null;
        int? physicalCoreCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Values.Sum() : coresPerSocket;
        return new PhdCpu
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = physicalProcessorCount,
            PhysicalCoreCount = physicalCoreCount,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequency?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequency?.Hertz.RoundToLong()
        };
    }

    internal static Frequency? ParseFrequencyFromBrandString(string brandString)
    {
        const string pattern = "(\\d.\\d+)GHz";
        var matches = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase);
        if (matches.Count > 0 && matches[0].Groups.Count > 1)
        {
            string match = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase)[0].Groups[1].ToString();
            return Frequency.TryParseGHz(match, out var result) ? result : null;
        }

        return null;
    }
}