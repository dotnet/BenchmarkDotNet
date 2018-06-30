using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
                    nominalFrequency = ParseFrequencyFromBrandString(modelName) * 1_000;
                    logicalCoreCount++;
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

        private static double ParseFrequencyFromBrandString(string brandString)
        {
            var pattern = "(\\d.\\d+)GHz";
            var matches = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0 && matches[0].Groups.Count > 1)
            {
                var match = Regex.Matches(brandString, pattern, RegexOptions.IgnoreCase)[0].Groups[1].ToString();
                return double.TryParse(match,NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ? result : 0d;
            }

            return 0d;
        }
    }
}