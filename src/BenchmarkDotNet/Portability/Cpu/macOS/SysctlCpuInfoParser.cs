using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Portability.Cpu.macOS;

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
    internal static CpuInfo Parse(string? sysctlOutput)
    {
        var sysctl = SectionsHelper.ParseSection(sysctlOutput, ':');
        string processorName = sysctl.GetValueOrDefault(Sysctl.ProcessorName);
        int? physicalProcessorCount = GetPositiveIntValue(sysctl, Sysctl.PhysicalProcessorCount);
        int? physicalCoreCount = GetPositiveIntValue(sysctl, Sysctl.PhysicalCoreCount);
        int? logicalCoreCount = GetPositiveIntValue(sysctl, Sysctl.LogicalCoreCount);
        long? nominalFrequency = GetPositiveLongValue(sysctl, Sysctl.NominalFrequency);
        long? maxFrequency = GetPositiveLongValue(sysctl, Sysctl.MaxFrequency);
        return new CpuInfo(
            processorName,
            physicalProcessorCount, physicalCoreCount, logicalCoreCount,
            nominalFrequency, maxFrequency);
    }

    private static int? GetPositiveIntValue(Dictionary<string, string> sysctl, string keyName)
    {
        if (sysctl.TryGetValue(keyName, out string value) &&
            int.TryParse(value, out int result) &&
            result > 0)
            return result;
        return null;
    }

    private static long? GetPositiveLongValue(Dictionary<string, string> sysctl, string keyName)
    {
        if (sysctl.TryGetValue(keyName, out string value) &&
            long.TryParse(value, out long result) &&
            result > 0)
            return result;
        return null;
    }
}