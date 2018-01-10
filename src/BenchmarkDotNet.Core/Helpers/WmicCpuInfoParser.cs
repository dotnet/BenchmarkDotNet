using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    public class WmicCpuInfoParser : ICpuInfo
    {
        public WmicCpuInfoParser(string content)
        {
            var processors = Parse(content, '=');

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

            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : (int?) null;
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : (int?) null;
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : (int?) null;
            ProcessorName = processorModelNames.Count == 0 ? null : string.Join(", ", processorModelNames);
        }
        
        public int? PhysicalCoreCount { get; }
        public int? PhysicalProcessorCount { get; }
        public int? LogicalCoreCount { get; }
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