using System.Collections.Generic;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    public class WmicCpuInfoParser : ICpuInfo
    {
        public WmicCpuInfoParser(string content)
        {
            var processors = StringHelper.ParseList(content, '=');

            var processorModelNames = new HashSet<string>();
            int physicalCoreCount = 0;
            int logicalCoreCount = 0;
            int processorsCount = 0;
            
            foreach (var processor in processors)
            {
                if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfCores, out string numberOfCoresValue) &&
                    int.TryParse(numberOfCoresValue, out int numberOfCores) &&
                    numberOfCores > 0)
                    physicalCoreCount += numberOfCores;

                if (processor.TryGetValue(WmicCpuInfoKeyNames.NumberOfLogicalProcessors, out string numberOfLogicalValue) &&
                    int.TryParse(numberOfLogicalValue, out int numberOfLogical) &&
                    numberOfLogical > 0)
                    logicalCoreCount += numberOfLogical;

                if (processor.TryGetValue(WmicCpuInfoKeyNames.Name, out string name))
                {
                    processorModelNames.Add(name);
                    processorsCount++;
                }
            }

            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : (int?) null;
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : (int?) null;
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : (int?) null;
        }

        public string ProcessorName { get; }
        public int? PhysicalCoreCount { get; }
        public int? PhysicalProcessorCount { get; }
        public int? LogicalCoreCount { get; }
    }
}