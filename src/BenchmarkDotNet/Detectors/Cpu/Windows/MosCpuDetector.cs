using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using Perfolizer.Horology;
using Perfolizer.Models;

namespace BenchmarkDotNet.Detectors.Cpu.Windows;

internal class MosCpuDetector : ICpuDetector
{
    [SupportedOSPlatform("windows")]
    public bool IsApplicable() => OsDetector.IsWindows() &&
                                  RuntimeInformation.IsFullFramework &&
                                  !RuntimeInformation.IsMono;

    [SupportedOSPlatform("windows")]
    public CpuInfo? Detect()
    {
        if (!IsApplicable()) return null;

        var processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorsCount = 0;
        double maxFrequency = 0;
        double nominalFrequency = 0;

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
                    double tempMaxFrequency = (uint)moProcessor[WmicCpuInfoKeyNames.MaxClockSpeed];

                    if (tempMaxFrequency > 0)
                    {
                        nominalFrequency = nominalFrequency == 0 ? tempMaxFrequency : Math.Min(nominalFrequency, tempMaxFrequency);
                    }
                    maxFrequency = Math.Max(maxFrequency, tempMaxFrequency);
                }
            }
        }

        string processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        Frequency? maxFrequencyActual = maxFrequency > 0 && processorsCount > 0
            ? Frequency.FromMHz(maxFrequency)
            : null;
        Frequency? nominalFrequencyActual = nominalFrequency > 0 && processorsCount > 0
            ? Frequency.FromMHz(nominalFrequency)
            : null;

        return new CpuInfo
        {
            ProcessorName = processorName,
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : null,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = nominalFrequencyActual?.Hertz.RoundToLong(),
            MaxFrequencyHz = maxFrequencyActual?.Hertz.RoundToLong()
        };
    }
}