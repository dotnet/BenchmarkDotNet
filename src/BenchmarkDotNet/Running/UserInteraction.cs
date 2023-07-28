using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class UserInteraction : IUserInteraction
    {
        private static bool consoleCancelKeyPressed;

        static UserInteraction() => Console.CancelKeyPress += (_, __) => consoleCancelKeyPressed = true;

        public void PrintNoBenchmarksError(ILogger logger)
        {
            logger.WriteError("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.");
        }

        public IReadOnlyList<Type> AskUser(IReadOnlyList<Type> allTypes, ILogger logger)
        {
            var selectedTypes = new List<Type>();
            string benchmarkCaptionExample = allTypes.First().GetDisplayName();

            while (selectedTypes.Count == 0 && !consoleCancelKeyPressed)
            {
                PrintAvailable(allTypes, logger);

                if (consoleCancelKeyPressed)
                    break;

                string filterExample = "--filter " + UserInteractionHelper.EscapeCommandExample($"*{benchmarkCaptionExample}*");
                logger.WriteLineHelp($"You should select the target benchmark(s). Please, print a number of a benchmark (e.g. `0`) or a contained benchmark caption (e.g. `{benchmarkCaptionExample}`).");
                logger.WriteLineHelp("If you want to select few, please separate them with space ` ` (e.g. `1 2 3`).");
                logger.WriteLineHelp($"You can also provide the class name in console arguments by using --filter. (e.g. `{filterExample}`).");
                logger.WriteLineHelp($"Enter the asterisk `*` to select all.");

                string userInput = Console.ReadLine();
                if (userInput == null)
                {
                    break;
                }

                selectedTypes.AddRange(GetMatching(allTypes, userInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                logger.WriteLine();
            }

            return selectedTypes;
        }

        public void PrintWrongFilterInfo(IReadOnlyList<Type> allTypes, ILogger logger, string[] userFilters)
        {
            var correctionSuggester = new CorrectionsSuggester(allTypes);

            var filterToNames = userFilters
                .Select(userFilter => (userFilter: userFilter, suggestedBenchmarkNames: correctionSuggester.SuggestFor(userFilter)))
                .ToArray();

            foreach ((string userFilter, var suggestedBenchmarkNames) in filterToNames)
                if (!suggestedBenchmarkNames.IsEmpty())
                {
                    logger.WriteLine($"You must have made a typo in '{userFilter}'. Suggestions:");
                    foreach (string displayName in suggestedBenchmarkNames.Take(40))
                        logger.WriteLineInfo($"\t{displayName}");
                }

            var unknownFilters = filterToNames.Where(u => u.suggestedBenchmarkNames.IsEmpty()).Select(u => u.userFilter).ToArray();
            string unknownBenchmarks = string.Join("', '", unknownFilters);

            if (!string.IsNullOrEmpty(unknownBenchmarks))
            {
                logger.WriteLineError($"{(unknownFilters.Length == 1 ? "The filter" : "Filters")} '{unknownBenchmarks}' that you have provided returned 0 benchmarks.");
                logger.WriteLineInfo("Please remember that the filter is applied to full benchmark name: `namespace.typeName.methodName`.");

                foreach (string displayName in correctionSuggester.GetAllBenchmarkNames().Take(40))
                    logger.WriteLineInfo($"\t{displayName}");
            }

            logger.WriteLineInfo("To print all available benchmarks use `--list flat` or `--list tree`.");
            logger.WriteLineInfo("To learn more about filtering use `--help`.");
        }

        private static IEnumerable<Type> GetMatching(IReadOnlyList<Type> allTypes, string[] userInput)
        {
            if (userInput.IsEmpty())
                yield break;

            var integerInput = userInput.Where(arg => IsInteger(arg)).ToArray();
            var stringInput = userInput.Where(arg => !IsInteger(arg)).ToArray();

            for (int i = 0; i < allTypes.Count; i++)
            {
                var type = allTypes[i];

                if (stringInput.Any(arg => type.GetDisplayName().ContainsWithIgnoreCase(arg))
                    || stringInput.Contains($"#{i}")
                    || integerInput.Contains($"{i}")
                    || stringInput.Contains("*"))
                {
                    yield return type;
                }
            }

            static bool IsInteger(string str) => int.TryParse(str, out _);
        }

        private static void PrintAvailable(IReadOnlyList<Type> allTypes, ILogger logger)
        {
            logger.WriteLineHelp($"Available Benchmark{(allTypes.Count > 1 ? "s" : "")}:");

            int numberWidth = allTypes.Count.ToString().Length;
            for (int i = 0; i < allTypes.Count && !consoleCancelKeyPressed; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), allTypes[i].GetDisplayName()));

            if (!consoleCancelKeyPressed)
            {
                logger.WriteLine();
                logger.WriteLine();
            }
        }
    }
}