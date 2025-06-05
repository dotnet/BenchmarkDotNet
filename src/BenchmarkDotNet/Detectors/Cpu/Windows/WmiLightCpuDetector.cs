using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;

using Perfolizer.Horology;
using Perfolizer.Models;

using System.Collections.Generic;

using WmiLight;

namespace BenchmarkDotNet.Detectors.Cpu.Windows
{
    internal class WmiLightCpuDetector : ICpuDetector
    {

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public CpuInfo? Detect()
        {
            if (!IsApplicable()) return null;

            HashSet<string> processorModelNames = new HashSet<string>();
            int physicalCoreCount = 0;
            int logicalCoreCount = 0;
            int processorsCount = 0;
            int sumMaxFrequency = 0;

            using (WmiConnection connection = new WmiConnection())
            {
                foreach (WmiObject processor in connection.CreateQuery("SELECT * FROM Win32_Processor"))
                {
                    string name = processor[WmicCpuInfoKeyNames.Name]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        processorModelNames.Add(name);
                        processorsCount++;
                        physicalCoreCount += (int)(uint)processor[WmicCpuInfoKeyNames.NumberOfCores];
                        logicalCoreCount += (int)(uint)processor[WmicCpuInfoKeyNames.NumberOfLogicalProcessors];
                        sumMaxFrequency = (int)(uint)processor[WmicCpuInfoKeyNames.MaxClockSpeed];
                    }
                }
            }

            string processorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
            Frequency? maxFrequency = sumMaxFrequency > 0 && processorsCount > 0
                ? Frequency.FromMHz(sumMaxFrequency * 1.0 / processorsCount)
                : null;

            return new CpuInfo
            {
                ProcessorName = processorName,
                PhysicalProcessorCount = processorsCount > 0 ? processorsCount : null,
                PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
                LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
                NominalFrequencyHz = maxFrequency?.Hertz.RoundToLong(),
                MaxFrequencyHz = maxFrequency?.Hertz.RoundToLong()
            };
        }

#if NET6_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public bool IsApplicable()
        {
            return OsDetector.IsWindows() && (RuntimeInformation.IsNetCore ||
                RuntimeInformation.IsFullFramework || RuntimeInformation.IsNativeAOT) && !RuntimeInformation.IsMono;
        }
    }
}
