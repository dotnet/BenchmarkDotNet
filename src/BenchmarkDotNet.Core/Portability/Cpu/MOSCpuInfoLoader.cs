#if !CORE
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace BenchmarkDotNet.Portability.Cpu
{
    public class MOSCpuInfoLoader : ICpuInfo
    {
        public MOSCpuInfoLoader()
        {
            var processorModelNames = new HashSet<string>();
            uint physicalCoreCount = 0;
            uint logicalCoreCount = 0;
            int processorsCount = 0;

            var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
            {
                var name = moProcessor[WmicCpuInfoKeyNames.Name]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    processorModelNames.Add(name);
                    processorsCount++;
                    physicalCoreCount += (uint) moProcessor[WmicCpuInfoKeyNames.NumberOfCores];
                    logicalCoreCount += (uint) moProcessor[WmicCpuInfoKeyNames.NumberOfLogicalProcessors];
                }
            }

            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : (int?) null;
            PhysicalCoreCount = physicalCoreCount > 0 ? (int?) physicalCoreCount : null;
            LogicalCoreCount = logicalCoreCount > 0 ? (int?) logicalCoreCount : null;
            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
        }

        public int? PhysicalCoreCount { get; }
        public int? PhysicalProcessorCount { get; }
        public int? LogicalCoreCount { get; }
        public string ProcessorName { get; }
    }
}
#endif