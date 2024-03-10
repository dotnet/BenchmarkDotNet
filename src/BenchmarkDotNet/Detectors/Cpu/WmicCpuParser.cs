using System.Collections.Generic;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Detectors.Cpu;

internal static class WmicCpuParser
{
    internal static PhdCpu ParseOutput(string? content)
    {
        var processors = SectionsHelper.ParseSections(content, '=');

        var processorModelNames = new HashSet<string>();
        int physicalCoreCount = 0;
        int logicalCoreCount = 0;
        int processorsCount = 0;

        var currentClockSpeed = Frequency.Zero;
        var maxClockSpeed = Frequency.Zero;
        var minClockSpeed = Frequency.Zero;

        foreach (var processor in processors)
        {
            if (processor.TryGetValue(WmicCpuKeyNames.NumberOfCores, out string numberOfCoresValue) &&
                int.TryParse(numberOfCoresValue, out int numberOfCores) &&
                numberOfCores > 0)
                physicalCoreCount += numberOfCores;

            if (processor.TryGetValue(WmicCpuKeyNames.NumberOfLogicalProcessors, out string numberOfLogicalValue) &&
                int.TryParse(numberOfLogicalValue, out int numberOfLogical) &&
                numberOfLogical > 0)
                logicalCoreCount += numberOfLogical;

            if (processor.TryGetValue(WmicCpuKeyNames.Name, out string name))
            {
                processorModelNames.Add(name);
                processorsCount++;
            }

            if (processor.TryGetValue(WmicCpuKeyNames.MaxClockSpeed, out string frequencyValue)
                && int.TryParse(frequencyValue, out int frequency)
                && frequency > 0)
            {
                maxClockSpeed += frequency;
            }
        }

        return new PhdCpu
        {
            ProcessorName = processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
            PhysicalProcessorCount = processorsCount > 0 ? processorsCount : null,
            PhysicalCoreCount = physicalCoreCount > 0 ? physicalCoreCount : null,
            LogicalCoreCount = logicalCoreCount > 0 ? logicalCoreCount : null,
            NominalFrequencyHz = currentClockSpeed > 0 && processorsCount > 0
                ? Frequency.FromMHz(currentClockSpeed / processorsCount).Hertz.RoundToLong()
                : null,
            MinFrequencyHz = minClockSpeed > 0 && processorsCount > 0
                ? Frequency.FromMHz(minClockSpeed / processorsCount).Hertz.RoundToLong()
                : null,
            MaxFrequencyHz = maxClockSpeed > 0 && processorsCount > 0
                ? Frequency.FromMHz(maxClockSpeed / processorsCount).Hertz.RoundToLong()
                : null
        };
    }
}