using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using BenchmarkDotNet.Extensions;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

internal static class MosCpuDetector
{
#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static readonly Lazy<PhdCpu> Cpu = new (Load);

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    private static PhdCpu Load()
    {
        var processorModelNames = new HashSet<string>();
        uint physicalCoreCount = 0;
        uint logicalCoreCount = 0;
        int processorsCount = 0;
        uint nominalClockSpeed = 0;
        uint maxClockSpeed = 0;
        uint minClockSpeed = 0;


        using (var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        {
            foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
            {
                string name = moProcessor[WmicCpuKeyNames.Name]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    processorModelNames.Add(name);
                    processorsCount++;
                    physicalCoreCount += (uint)moProcessor[WmicCpuKeyNames.NumberOfCores];
                    logicalCoreCount += (uint)moProcessor[WmicCpuKeyNames.NumberOfLogicalProcessors];
                    maxClockSpeed = (uint)moProcessor[WmicCpuKeyNames.MaxClockSpeed];
                }
            }
        }

        return new PhdCpu
        {
            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : (int?)null,
            PhysicalCoreCount = physicalCoreCount > 0 ? (int?)physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? (int?)logicalCoreCount : null,
            NominalFrequencyHz = nominalClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(nominalClockSpeed).Hertz.RoundToLong() : null,
            MinFrequencyHz = minClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(minClockSpeed).Hertz.RoundToLong() : null,
            MaxFrequencyHz = maxClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(maxClockSpeed).Hertz.RoundToLong() : null
        };
    }
}