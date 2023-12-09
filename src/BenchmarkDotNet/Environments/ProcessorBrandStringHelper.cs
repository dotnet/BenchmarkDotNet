using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability.Cpu;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Environments
{
    public static class ProcessorBrandStringHelper
    {
        /// <summary>
        /// Transform a processor brand string to a nice form for summary.
        /// </summary>
        /// <param name="cpuInfo">The CPU information</param>
        /// <param name="includeMaxFrequency">Whether to include determined max frequency information</param>
        /// <returns>Prettified version</returns>
        public static string Prettify(CpuInfo cpuInfo, bool includeMaxFrequency = false)
        {
            if (cpuInfo == null || string.IsNullOrEmpty(cpuInfo.ProcessorName))
            {
                return "Unknown processor";
            }

            // Remove parts which don't provide any useful information for user
            var processorName = cpuInfo.ProcessorName.Replace("@", "").Replace("(R)", "").Replace("(TM)", "");

            // If we have found physical core(s), we can safely assume we can drop extra info from brand
            if (cpuInfo.PhysicalCoreCount.HasValue && cpuInfo.PhysicalCoreCount.Value > 0)
                processorName = Regex.Replace(processorName, @"(\w+?-Core Processor)", "").Trim();

            string frequencyString = GetBrandStyledActualFrequency(cpuInfo.NominalFrequency);
            if (includeMaxFrequency && frequencyString != null && !processorName.Contains(frequencyString))
            {
                // show Max only if there's already a frequency name to differentiate the two
                string maxFrequency = processorName.Contains("Hz") ? $"(Max: {frequencyString})" : frequencyString;
                processorName = $"{processorName} {maxFrequency}";
            }

            // Remove double spaces
            processorName = Regex.Replace(processorName.Trim(), @"\s+", " ");

            // Add microarchitecture name if known
            string microarchitecture = ParseMicroarchitecture(processorName);
            if (microarchitecture != null)
                processorName = $"{processorName} ({microarchitecture})";

            return processorName;
        }

        /// <summary>
        /// Presents actual processor's frequency into brand string format
        /// </summary>
        /// <param name="frequency"></param>
        private static string GetBrandStyledActualFrequency(Frequency? frequency)
        {
            if (frequency == null)
                return null;
            return $"{frequency.Value.ToGHz().ToString("N2", DefaultCultureInfo.Instance)}GHz";
        }

        /// <summary>
        /// Parse a processor name and tries to return a microarchitecture name.
        /// Works only for well-known microarchitectures.
        /// </summary>
        private static string? ParseMicroarchitecture(string processorName)
        {
            if (processorName.StartsWith("Intel Core"))
            {
                string model = processorName.Substring("Intel Core".Length).Trim();

                // Core i3/5/7/9
                if (
                    model.Length > 4 &&
                    model[0] == 'i' &&
                    (model[1] == '3' || model[1] == '5' || model[1] == '7' || model[1] == '9') &&
                    (model[2] == '-' || model[2] == ' '))
                {
                    string modelNumber = model.Substring(3);
                    if (modelNumber.StartsWith("CPU"))
                        modelNumber = modelNumber.Substring(3).Trim();
                    if (modelNumber.Contains("CPU"))
                        modelNumber = modelNumber.Substring(0, modelNumber.IndexOf("CPU", StringComparison.Ordinal)).Trim();
                    return ParseIntelCoreMicroarchitecture(modelNumber);
                }
            }

            return null;
        }

        private static readonly Lazy<Dictionary<string, string>> KnownMicroarchitectures = new Lazy<Dictionary<string, string>>(() =>
        {
            var data = ResourceHelper.LoadResource("BenchmarkDotNet.Environments.microarchitectures.txt").Split('\r', '\n');
            var dictionary = new Dictionary<string, string>();
            string? currentMicroarchitecture = null;
            foreach (string line in data)
            {
               if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                   continue;
               if (line.StartsWith("#"))
               {
                   currentMicroarchitecture = line.Substring(1).Trim();
                   continue;
               }

               string modelNumber = line.Trim();
               if (dictionary.ContainsKey(modelNumber))
                   throw new Exception($"{modelNumber} is defined twice in microarchitectures.txt");
               if (currentMicroarchitecture == null)
                   throw new Exception($"{modelNumber} doesn't have defined microarchitecture in microarchitectures.txt");
               dictionary[modelNumber] = currentMicroarchitecture;
            }

            return dictionary;
        });

        // see http://www.intel.com/content/www/us/en/processors/processor-numbers.html
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal static string? ParseIntelCoreMicroarchitecture(string modelNumber)
        {
            if (KnownMicroarchitectures.Value.TryGetValue(modelNumber, out string? microarchitecture))
                return microarchitecture;

            if (modelNumber.Length >= 3 && modelNumber.Substring(0, 3).All(char.IsDigit) &&
                (modelNumber.Length == 3 || !char.IsDigit(modelNumber[3])))
                return "Nehalem";
            if (modelNumber.Length >= 4 && modelNumber.Substring(0, 4).All(char.IsDigit))
            {
                char generation = modelNumber[0];
                switch (generation)
                {
                    case '2':
                        return "Sandy Bridge";
                    case '3':
                        return "Ivy Bridge";
                    case '4':
                        return "Haswell";
                    case '5':
                        return "Broadwell";
                    case '6':
                        return "Skylake";
                    case '7':
                        return "Kaby Lake";
                    case '8':
                        return "Coffee Lake";
                    default:
                        return null;
                }
            }
            return null;
        }
    }
}