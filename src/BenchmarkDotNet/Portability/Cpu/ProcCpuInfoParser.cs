using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Portability.Cpu
{
    internal static class ProcCpuInfoParser
    {
        [NotNull]
        internal static CpuInfo ParseOutput([CanBeNull] string content)
        {
            var logicalCores = SectionsHelper.ParseSections(content, ':');
            var processorModelNames = new HashSet<string>();
            var processorsToPhysicalCoreCount = new Dictionary<string, int>();
            
            var logicalCoreCount = 0;
            var nominalFrequency = 0d;
            var minFrequency = 0d;
            var maxFrequency = 0d;
            
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

                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.NominalFrequency, out string cpuFreqValue)
                    && double.TryParse(cpuFreqValue.Replace(',','.'), out double cpuFreq))
                {
                    nominalFrequency = cpuFreq;
                }
                
                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.MinFrequency, out string minCpuFreqValue)
                    && double.TryParse(minCpuFreqValue.Replace(',','.'), out double minCpuFreq))
                {
                    minFrequency = minCpuFreq;
                }
                
                if (logicalCore.TryGetValue(ProcCpuInfoKeyNames.MaxFrequency, out string maxCpuFreqValue)
                     && double.TryParse(maxCpuFreqValue.Replace(',','.'), out double maxCpuFreq))
                {
                    maxFrequency = maxCpuFreq;
                }
            }

            return new CpuInfo(
                processorModelNames.Count > 0 ? string.Join(", ", processorModelNames) : null,
                processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Count : (int?) null,
                processorsToPhysicalCoreCount.Count > 0 ? processorsToPhysicalCoreCount.Values.Sum() : (int?) null,
                logicalCoreCount > 0 ? logicalCoreCount : (int?) null,
                nominalFrequency > 0 ? nominalFrequency : (double?) null,
                minFrequency > 0 ? minFrequency : (double?) null,
                maxFrequency > 0 ? maxFrequency : (double?) null);
        }
    }
}