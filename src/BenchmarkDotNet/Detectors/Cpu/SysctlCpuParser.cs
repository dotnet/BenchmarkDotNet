using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

internal static class SysctlCpuParser
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static PhdCpu Parse(string? content)
    {
        var sysctl = SectionsHelper.ParseSection(content, ':');
        return new PhdCpu
        {
            ProcessorName = sysctl.GetValueOrDefault("machdep.cpu.brand_string"),
            PhysicalProcessorCount = GetPositiveIntValue(sysctl, "hw.packages"),
            PhysicalCoreCount = GetPositiveIntValue(sysctl, "hw.physicalcpu"),
            LogicalCoreCount = GetPositiveIntValue(sysctl, "hw.logicalcpu"),
            NominalFrequencyHz = GetPositiveLongValue(sysctl, "hw.cpufrequency"),
            MinFrequencyHz = GetPositiveLongValue(sysctl, "hw.cpufrequency_min"),
            MaxFrequencyHz = GetPositiveLongValue(sysctl, "hw.cpufrequency_max")
        };
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