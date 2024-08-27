using System.Collections.Generic;
using System.Linq;
using System.Management;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class MosCpuDetector : ICpuDetector
{
#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public bool IsApplicable() => OsDetector.IsWindows() &&
                                  RuntimeInformation.IsFullFramework &&
                                  !RuntimeInformation.IsMono;

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public PhdCpu? Detect()
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