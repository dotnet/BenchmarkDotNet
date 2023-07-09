using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class MosCpuInfoProvider
    {
#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        internal static readonly Lazy<CpuInfo> MosCpuInfo = new Lazy<CpuInfo>(Load);

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        private static CpuInfo Load()
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
                    string name = moProcessor[WmicCpuInfoKeyNames.Name]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        processorModelNames.Add(name);
                        processorsCount++;
                        physicalCoreCount += (uint) moProcessor[WmicCpuInfoKeyNames.NumberOfCores];
                        logicalCoreCount += (uint) moProcessor[WmicCpuInfoKeyNames.NumberOfLogicalProcessors];
                        maxClockSpeed = (uint) moProcessor[WmicCpuInfoKeyNames.MaxClockSpeed];
                    }
                }
            }

            return new CpuInfo(
                processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
                processorsCount > 0 ? processorsCount : (int?) null,
                physicalCoreCount > 0 ? (int?) physicalCoreCount : null,
                logicalCoreCount > 0 ? (int?) logicalCoreCount : null,
                nominalClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(nominalClockSpeed) : (Frequency?) null,
                minClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(minClockSpeed) : (Frequency?) null,
                maxClockSpeed > 0 && logicalCoreCount > 0 ? Frequency.FromMHz(maxClockSpeed) : (Frequency?) null);
        }
    }
}