using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal static class UserInteraction
    {
        private static bool consoleCancelKeyPressed;
        
        static UserInteraction() => Console.CancelKeyPress += (_, __) => consoleCancelKeyPressed = true;

        internal static void PrintNoBenchmarksError(ILogger logger)
        {
            logger.WriteError("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.");
        }

        internal static IReadOnlyList<Type> AskUser(IReadOnlyList<Type> allTypes, ILogger logger)
        {
            var selectedTypes = new List<Type>();
            string benchmarkCaptionExample = allTypes.First().GetDisplayName();

            while (selectedTypes.Count == 0  && !consoleCancelKeyPressed)
            {
                PrintAvailable(allTypes, logger);
                
                if (consoleCancelKeyPressed)
                    break;

                logger.WriteLineHelp($"You should select the target benchmark(s). Please, print a number of a benchmark (e.g. '0') or a contained benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                logger.WriteLineHelp("If you want to select few, please separate them with space ` ` (e.g. `1 2 3`)");
                logger.WriteLineHelp($"You can also provide the class name in console arguments by using --filter. (e.g. '--filter *{benchmarkCaptionExample}*'):");

                string userInput = Console.ReadLine() ?? "";

                selectedTypes.AddRange(GetMatching(allTypes, userInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                logger.WriteLine();
            }

            return selectedTypes;
        }

        internal static void PrintWrongFilterInfo(IReadOnlyList<Type> allTypes, ILogger logger)
        {
            logger.WriteLineError("The filter that you have provided returned 0 benchmarks.");
            logger.WriteLineInfo("Please remember that the filter is applied to full benchmark name: `namespace.typeName.methodName`.");
            logger.WriteLineInfo("Some examples of full names:");
            
            foreach (string displayName in allTypes
                .SelectMany(type => BenchmarkConverter.TypeToBenchmarks(type, DefaultConfig.Instance).BenchmarksCases) // we use DefaultConfig to NOT filter the benchmarks
                .Select(benchmarkCase => benchmarkCase.Descriptor.GetFilterName())
                .Distinct()
                .OrderBy(displayName => displayName)
                .Take(40))
            {
                logger.WriteLineInfo($"\t{displayName}");
            }

            logger.WriteLineInfo("To print all available benchmarks use `--list flat` or `--list tree`.");
            logger.WriteLineInfo("To learn more about filtering use `--help`.");
        }

        private static IEnumerable<Type> GetMatching(IReadOnlyList<Type> allTypes, string[] userInput)
        {
            if (userInput.IsEmpty())
                yield break;

            for (int i = 0; i < allTypes.Count; i++)
            {
                var type = allTypes[i];

                if (userInput.Any(arg => type.GetDisplayName().ContainsWithIgnoreCase(arg))
                    || userInput.Contains($"#{i}")
                    || userInput.Contains(i.ToString())
                    || userInput.Contains("*"))
                {
                    yield return type;
                }
            }
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
