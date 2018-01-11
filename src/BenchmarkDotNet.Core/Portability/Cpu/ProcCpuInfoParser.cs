using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    public class ProcCpuInfoParser : ICpuInfo
    {
        public ProcCpuInfoParser(string content)
        {
            var logicalCores = StringHelper.ParseList(content, ':');
            var logicalCoreCount = 0;
            var processorsToPhysicalCoresCount = new Dictionary<string, int>();
            var processorModelNames = new HashSet<string>();
            foreach (var logicalCore in logicalCores)
            {
                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.PhysicalId, out string physicalId) &&
                    logicalCore.TryGetValue(ProcCpuInfoKeyNames.CpuCores, out string cpuCoresValue) &&
                    int.TryParse(cpuCoresValue, out int cpuCoresCount))
                    processorsToPhysicalCoresCount[physicalId] = cpuCoresCount;

                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.ModelName, out string modelName))
                {
                    processorModelNames.Add(modelName);
                    logicalCoreCount++;
                }
            }

            PhysicalProcessorCount = processorsToPhysicalCoresCount.Count > 0 ? processorsToPhysicalCoresCount.Count : (int?) null;
            PhysicalCoreCount = processorsToPhysicalCoresCount.Count > 0 ? processorsToPhysicalCoresCount.Values.Sum() : (int?) null;
            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : (int?) null;
        }

        public int? PhysicalCoreCount { get; }

        public int? PhysicalProcessorCount { get; }

        public int? LogicalCoreCount { get; }

        public string ProcessorName { get; }
    }
}