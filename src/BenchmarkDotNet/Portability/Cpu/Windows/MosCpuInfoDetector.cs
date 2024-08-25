using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Portability.Cpu.Windows;

internal class MosCpuInfoDetector : ICpuInfoDetector
{
#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public bool IsApplicable() => RuntimeInformation.IsWindows() &&
                                  RuntimeInformation.IsFullFramework &&
                                  !RuntimeInformation.IsMono;

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        var processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorsCount = 0;
        int sumMaxFrequency = 0;

        using (var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        {
            foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
            {
                string name = moProcessor[WmicCpuInfoKeyNames.Name]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    processorModelNames.Add(name);
                    processorsCount++;
                    physicalCoreCount += (int)(uint)moProcessor[WmicCpuInfoKeyNames.NumberOfCores];
                    logicalCoreCount += (int)(uint)moProcessor[WmicCpuInfoKeyNames.NumberOfLogicalProcessors];
                    sumMaxFrequency = (int)(uint)moProcessor[WmicCpuInfoKeyNames.MaxClockSpeed];
                }
            }
        }

        string processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        Frequency? maxFrequency = sumMaxFrequency > 0 && processorsCount > 0
            ? Frequency.FromMHz(sumMaxFrequency * 1.0 / processorsCount)
            : null;

        return new CpuInfo(
            processorName,
            GetCount(processorsCount), GetCount(physicalCoreCount), GetCount(logicalCoreCount),
            maxFrequency, maxFrequency);

        int? GetCount(int count) => count > 0 ? count : null;
    }
}