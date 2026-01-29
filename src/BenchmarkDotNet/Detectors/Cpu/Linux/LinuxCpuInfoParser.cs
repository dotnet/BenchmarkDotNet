using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Linux;

#nullable enable

internal static class LinuxCpuInfoParser
{
    private static class ProcCpu
    {
        internal const string PhysicalId = "physical id";
        internal const string CpuCores = "cpu cores";
        internal const string ModelName = "model name";
        internal const string MaxFrequency = "max freq";
        internal const string NominalFrequencyBackup = "nominal freq";
        internal const string NominalFrequency = "cpu MHz";
    }

    private static class Lscpu
    {
        internal const string MaxFrequency = "CPU max MHz";
        internal const string ModelName = "Model name";
        internal const string CoresPerSocket = "Core(s) per socket";
    }

    /// <param name="cpuInfo">Output of `cat /proc/cpuinfo`</param>
    /// <param name="lscpu">Output of `lscpu`</param>
    internal static CpuInfo Parse(string cpuInfo, string lscpu)
    {
        var processorModelNames = new HashSet<string>();
        var processorsToPhysicalCoreCount = new Dictionary<string, int>();
        int logicalCoreCount = 0;
        double maxFrequency = 0.0;
        double nominalFrequency = 0.0;

        var logicalCores = SectionsHelper.ParseSections(cpuInfo, ':');
        foreach (var logicalCore in logicalCores)
        {
            if (logicalCore.TryGetValue(ProcCpu.PhysicalId, out var physicalId) &&
                logicalCore.TryGetValue(ProcCpu.CpuCores, out var cpuCoresValue) &&
                int.TryParse(cpuCoresValue, out int cpuCoreCount) &&
                cpuCoreCount > 0)
                processorsToPhysicalCoreCount[physicalId] = cpuCoreCount;

            if (logicalCore.TryGetValue(ProcCpu.ModelName, out var modelName))
            {
                processorModelNames.Add(modelName);
                logicalCoreCount++;
            }

            if (logicalCore.TryGetValue(ProcCpu.MaxFrequency, out var maxCpuFreqValue) &&
                Frequency.TryParseMHz(maxCpuFreqValue.Replace(',', '.'), out Frequency maxCpuFreq)
                && maxCpuFreq > 0)
            {
                maxFrequency = Math.Max(maxFrequency, maxCpuFreq.ToMHz());
            }

            bool nominalFrequencyHasValue = logicalCore.TryGetValue(ProcCpu.NominalFrequency, out var nominalFreqValue);
            bool nominalFrequencyBackupHasValue = logicalCore.TryGetValue(ProcCpu.NominalFrequencyBackup, out var nominalFreqBackupValue);

            double nominalCpuFreq = 0.0;
            double nominalCpuBackupFreq = 0.0;

            if (nominalFrequencyHasValue &&
                double.TryParse(nominalFreqValue, out nominalCpuFreq)
                && nominalCpuFreq > 0)
            {
                nominalCpuFreq = nominalFrequency == 0 ? nominalCpuFreq : Math.Min(nominalFrequency, nominalCpuFreq);
            }
            if (nominalFrequencyBackupHasValue &&
                     double.TryParse(nominalFreqBackupValue, out nominalCpuBackupFreq)
                     && nominalCpuBackupFreq > 0)
            {
                nominalCpuBackupFreq = nominalFrequency == 0 ? nominalCpuBackupFreq : Math.Min(nominalFrequency, nominalCpuBackupFreq);
            }

            if (nominalFrequencyHasValue && nominalFrequencyBackupHasValue)
            {
                nominalFrequency = Math.Min(nominalCpuFreq, nominalCpuBackupFreq);
            }
            else
            {
                nominalFrequency = nominalCpuFreq == 0.0 ? nominalCpuBackupFreq : nominalCpuFreq;
            }
        }

        int? coresPerSocket = null;
        if (lscpu.IsNotBlank())
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
                    Frequency.TryParseMHz(value.Replace(',', '.'), out Frequency maxFrequencyParsed)) // Example: `CPU max MHz: 3200,0000`
                    maxFrequency = Math.Max(maxFrequency, maxFrequencyParsed.ToMHz());

                if (name.EqualsWithIgnoreCase(Lscpu.ModelName))
                    processorModelNames.Add(value);

                if (name.EqualsWithIgnoreCase(Lscpu.CoresPerSocket) &&
                    int.TryParse(value, out int coreCount))
                    coresPerSocket = coreCount;
            }
        }

        string? processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        int? physicalProcessorCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Count : null;
        int? physicalCoreCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Values.Sum() : coresPerSocket;

        Frequency? maxFrequencyActual = maxFrequency > 0 && physicalProcessorCount > 0
            ? Frequency.FromMHz(maxFrequency) : null;

        Frequency? nominalFrequencyActual = nominalFrequency > 0 && physicalProcessorCount > 0
            ? Frequency.FromMHz(nominalFrequency) : null;

        if (nominalFrequencyActual is null)
        {
            bool nominalFrequencyInBrandString = processorModelNames.Any(x => ParseFrequencyFromBrandString(x) is not null);

            if (nominalFrequencyInBrandString)
                nominalFrequencyActual = processorModelNames.Select(x => ParseFrequencyFromBrandString(x))
                    .First(x => x is not null);
        }

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = physicalProcessorCount,
            PhysicalCoreCount = physicalCoreCount,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong()
        };
    }

    internal static Frequency? ParseFrequencyFromBrandString(string brandString)
    {
        const string pattern = "(\\d.\\d+)GHz";
        var matches = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase);
        if (matches.Count > 0 && matches[0].Groups.Count > 1)
        {
            string match = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase)[0].Groups[1].ToString();
            return Frequency.TryParseGHz(match, out Frequency result) ? result : null;
        }

        return null;
    }
}