using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    public class ProcCpuInfoParser
    {
        public ProcCpuInfoParser(string content)
        {
            var logicalCores = Parse(content, ':');
            var processorsToPhysicalCoresCount = new Dictionary<string, int>();
            var processorModelNames = new HashSet<string>();
            foreach (var logicalCore in logicalCores)
            {
                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.PhysicalId, out string physicalId) &&
                    logicalCore.TryGetValue(ProcCpuInfoKeyNames.CpuCores, out string cpuCoresValue) &&
                    int.TryParse(cpuCoresValue, out int cpuCoresCount))
                    processorsToPhysicalCoresCount[physicalId] = cpuCoresCount;

                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.ModelName, out string modelName))
                    processorModelNames.Add(modelName);
            }

            PhysicalProcessorCount = processorsToPhysicalCoresCount.Count == 0 ? (int?) null : processorsToPhysicalCoresCount.Count;
            PhysicalCoreCount = processorsToPhysicalCoresCount.Count == 0 ? (int?) null : processorsToPhysicalCoresCount.Values.Sum();
            ProcessorName = processorModelNames.Count == 0 ? null : string.Join(", ", processorModelNames);
        }

        public int? PhysicalCoreCount { get; }

        public int? PhysicalProcessorCount { get; }

        public string ProcessorName { get; }

        [NotNull]
        private static List<Dictionary<string, string>> Parse([CanBeNull] string content, char separator)
        {
            var items = new List<Dictionary<string, string>>();
            Dictionary<string, string> units = null;

            var list = Regex.Split(content ?? "", "\r?\n");
            foreach (string line in list)
                if (line.IndexOf(separator) != -1)
                {
                    var lineParts = line.Split(separator);
                    if (lineParts.Length >= 2)
                    {
                        if (units == null)
                            items.Add(units = new Dictionary<string, string>());
                        units[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
                }
                else
                    units = null;
            return items;
        }

    }
}