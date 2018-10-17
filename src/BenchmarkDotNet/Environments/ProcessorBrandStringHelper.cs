using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability.Cpu;
using JetBrains.Annotations;

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
        [NotNull]
        public static string Prettify(CpuInfo cpuInfo, bool includeMaxFrequency = false)
        {
            if (cpuInfo == null)
            {
                return "";
            }

            // Remove parts which don't provide any useful information for user
            var processorName = cpuInfo.ProcessorName.Replace("@", "").Replace("(R)", "").Replace("(TM)", "");
            
            // If we have found physical core(s), we can safely assume we can drop extra info from brand
            if (cpuInfo.PhysicalCoreCount > 0)
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
            return $"{frequency.Value.ToGHz().ToStr("N2")}GHz";
        }

        /// <summary>
        /// Parse a processor name and tries to return a microarchitecture name.
        /// Works only for well-known microarchitectures.
        /// </summary>
        [CanBeNull]
        private static string ParseMicroarchitecture([NotNull] string processorName)
        {
            if (processorName.StartsWith("Intel Core"))
            {
                string model = processorName.Substring("Intel Core".Length).Trim();

                // Core i3/5/7
                if (
                    model.Length > 4 &&
                    model[0] == 'i' &&
                    (model[1] == '3' || model[1] == '5' || model[1] == '7') &&
                    (model[2] == '-' || model[2] == ' '))
                {
                    string modelNumber = model.Substring(3);
                    if (modelNumber.StartsWith("CPU"))
                        modelNumber = modelNumber.Substring(3).Trim();
                    return ParseIntroCoreMicroarchitecture(modelNumber);
                }
            }

            return null;
        }

        // see http://www.intel.com/content/www/us/en/processors/processor-numbers.html
        [CanBeNull]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static string ParseIntroCoreMicroarchitecture([NotNull] string modelNumber)
        {
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
                    {
                        if (modelNumber.Length >= 5 && modelNumber[4] == 'U')
                            return "Kaby Lake R";
                        return "Coffee Lake";
                    }
                    default:
                        return null;
                }
            }
            return null;
        }
    }
}