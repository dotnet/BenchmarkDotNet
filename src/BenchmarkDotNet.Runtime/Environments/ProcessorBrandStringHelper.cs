using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Environments
{
    public static class ProcessorBrandStringHelper
    {
        /// <summary>
        /// Transform a processor brand string to a nice form for summary.
        /// </summary>
        /// <param name="processorName">Original processor brand string</param>
        /// <returns>Prettified version</returns>
        [NotNull]
        public static string Prettify([NotNull] string processorName)
        {
            // Remove parts which don't provide any useful information for user
            processorName = processorName.Replace("@", "").Replace("(R)", "").Replace("(TM)", "");

            // Remove double spaces
            processorName = Regex.Replace(processorName.Trim(), @"\s+", " ");

            // Add microarchitecture name if known
            string microarchitecture = ParseMicroarchitecture(processorName);
            if (microarchitecture != null)
                processorName = $"{processorName} ({microarchitecture})";

            return processorName;
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
                    default:
                        return null;
                }
            }
            return null;
        }
    }
}