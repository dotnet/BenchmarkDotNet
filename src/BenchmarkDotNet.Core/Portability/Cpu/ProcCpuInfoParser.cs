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
            var processorModelNames = new HashSet<string>();
            var processorsToPhysicalCoreCount = new Dictionary<string, int>();
            var logicalCoreCount = 0;
            foreach (var logicalCore in logicalCores)
            {
                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.PhysicalId, out string physicalId) &&
                    logicalCore.TryGetValue(ProcCpuInfoKeyNames.CpuCores, out string cpuCoresValue) &&
                    int.TryParse(cpuCoresValue, out int cpuCoreCount) &&
                    cpuCoreCount > 0)
                    processorsToPhysicalCoreCount[physicalId] = cpuCoreCount;

                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.ModelName, out string modelName))
                {
                    processorModelNames.Add(modelName);
                    logicalCoreCount++;
                }
            }

            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null;
            PhysicalProcessorCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Count : (int?) null;
            PhysicalCoreCount = processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Values.Sum() : (int?) null;
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : (int?) null;
        }

        public string ProcessorName { get; }
        public int? PhysicalProcessorCount { get; }
        public int? PhysicalCoreCount { get; }
        public int? LogicalCoreCount { get; }
    }
}