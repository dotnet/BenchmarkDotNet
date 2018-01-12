using System.Collections.Generic;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class WmicCpuInfoParser
    {
        [NotNull]
        internal static CpuInfo ParseOutput([CanBeNull] string content)
        {
            var processors = SectionsHelper.ParseSections(content, '=');

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

            return new CpuInfo(
                processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
                processorsCount > 0 ? processorsCount : (int?) null,
                physicalCoreCount > 0 ? physicalCoreCount : (int?) null,
                logicalCoreCount > 0 ? logicalCoreCount : (int?) null);
        }
    }
}