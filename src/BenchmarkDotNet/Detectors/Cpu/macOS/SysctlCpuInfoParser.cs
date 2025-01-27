using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.macOS;

internal static class SysctlCpuInfoParser
{
    private static class Sysctl
    {
        internal const string ProcessorName = "machdep.cpu.brand_string";
        internal const string PhysicalProcessorCount = "hw.packages";
        internal const string PhysicalCoreCount = "hw.physicalcpu";
        internal const string LogicalCoreCount = "hw.logicalcpu";
        internal const string NominalFrequency = "hw.cpufrequency";
        internal const string MaxFrequency = "hw.cpufrequency_max";
    }

    /// <param name="sysctlOutput">Output of `sysctl -a`</param>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static PhdCpu Parse(string? sysctlOutput)
    {
        var sysctl = SectionsHelper.ParseSection(sysctlOutput, ':');
        string processorName = sysctl.GetValueOrDefault(Sysctl.ProcessorName);
        int? physicalProcessorCount = PositiveIntValue(sysctl, Sysctl.PhysicalProcessorCount);
        int? physicalCoreCount = PositiveIntValue(sysctl, Sysctl.PhysicalCoreCount);
        int? logicalCoreCount = PositiveIntValue(sysctl, Sysctl.LogicalCoreCount);
        long? nominalFrequency = PositiveLongValue(sysctl, Sysctl.NominalFrequency);
        long? maxFrequency = PositiveLongValue(sysctl, Sysctl.MaxFrequency);
        return new PhdCpu
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = physicalProcessorCount,
            PhysicalCoreCount = physicalCoreCount,
            LogicalCoreCount = logicalCoreCount,
            NominalFrequencyHz = nominalFrequency,
            MaxFrequencyHz = maxFrequency
        };
    }

    private static int? PositiveIntValue(Dictionary<string, string> sysctl, string keyName)
    {
        if (sysctl.TryGetValue(keyName, out string value) &&
            int.TryParse(value, out int result) &&
            result > 0)
            return result;
        return null;
    }

    private static long? PositiveLongValue(Dictionary<string, string> sysctl, string keyName)
    {
        if (sysctl.TryGetValue(keyName, out string value) &&
            long.TryParse(value, out long result) &&
            result > 0)
            return result;
        return null;
    }
}