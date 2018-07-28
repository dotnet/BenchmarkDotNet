using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Running
{
    internal class TypeParser
    {
        private static bool consoleCancelKeyPressed;
        
        private readonly Type[] allTypes;
        private readonly ILogger logger;

        static TypeParser() => Console.CancelKeyPress += (_, __) => consoleCancelKeyPressed = true; 

        internal TypeParser(Type[] types, ILogger logger)
        {
            this.logger = logger;
            allTypes = GenericBenchmarksBuilder.GetRunnableBenchmarks(types);
        }

        private class TypeWithMethods
        {
            public Type Type { get; }
            public MethodInfo[] Methods { get; }
            public bool AllMethodsInType { get; }

            public TypeWithMethods(Type type, MethodInfo[] methods = null)
            {
                Type = type;
                Methods = methods;
                AllMethodsInType = methods == null;
            }
        }

        internal BenchmarkRunInfo[] Filter(IConfig effectiveConfig)
        {
            if (allTypes.IsEmpty())
            {
                logger.WriteError("No benchmarks to choose from. Make sure you provided public non-sealed non-static types with public [Benchmark] methods.");
                return Array.Empty<BenchmarkRunInfo>();
            }

            bool hasFilters = effectiveConfig.GetFilters().Any();

            var benchmarks = (hasFilters ? GetAll() : AskUser()) // if user provided some filters via args or custom config , we don't ask for any input
                .Select(typeWithMethods =>
                    typeWithMethods.AllMethodsInType
                        ? BenchmarkConverter.TypeToBenchmarks(typeWithMethods.Type, effectiveConfig)
                        : BenchmarkConverter.MethodsToBenchmarks(typeWithMethods.Type, typeWithMethods.Methods, effectiveConfig))
                .Where(info => info.BenchmarksCases.Any())
                .ToArray();

            if (benchmarks.IsEmpty() && hasFilters)
                PrintWrongFilterInfo();

            return benchmarks;
        }

        private IEnumerable<TypeWithMethods> GetAll() => allTypes.Select(type => new TypeWithMethods(type));

        private IEnumerable<TypeWithMethods> AskUser()
        {
            var selectedTypes = new List<TypeWithMethods>();
            string benchmarkCaptionExample = allTypes.First().GetDisplayName();

            while (selectedTypes.Count == 0  && !consoleCancelKeyPressed)
            {
                PrintAvailable();
                
                if (consoleCancelKeyPressed)
                    break;

                logger.WriteLineHelp($"You should select the target benchmark(s). Please, print a number of a benchmark (e.g. '0') or a contained benchmark caption (e.g. '{benchmarkCaptionExample}'):");
                logger.WriteLineHelp("If you want to select few, please separate them with space ` ` (e.g. `1 2 3`)");
                logger.WriteLineHelp($"You can also provide the class name in console arguments by using --filter. (e.g. '--filter *{benchmarkCaptionExample}*'):");

                string userInput = Console.ReadLine() ?? "";

                selectedTypes.AddRange(GetMatching(userInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                logger.WriteLine();
            }

            return selectedTypes;
        }

        private IEnumerable<TypeWithMethods> GetMatching(string[] userInput)
        {
            if (userInput.IsEmpty())
                yield break;

            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = allTypes[i];

                if (userInput.Any(arg => type.GetDisplayName().ContainsWithIgnoreCase(arg))
                    || userInput.Contains($"#{i}")
                    || userInput.Contains(i.ToString())
                    || userInput.Contains("*"))
                {
                    yield return new TypeWithMethods(type);
                }
            }
        }

        private void PrintAvailable()
        {
            if (allTypes.IsEmpty())
            {
                logger.WriteLineError("You don't have any benchmarks");
                return;
            }

            logger.WriteLineHelp($"Available Benchmark{(allTypes.Length > 1 ? "s" : "")}:");

            int numberWidth = allTypes.Length.ToString().Length;
            for (int i = 0; i < allTypes.Length && !consoleCancelKeyPressed; i++)
                logger.WriteLineHelp(string.Format(CultureInfo.InvariantCulture, "  #{0} {1}", i.ToString().PadRight(numberWidth), allTypes[i].GetDisplayName()));

            if (!consoleCancelKeyPressed)
            {
                logger.WriteLine();
                logger.WriteLine();
            }
        }

        private void PrintWrongFilterInfo()
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

            logger.WriteLineInfo("To learn more about filtering use `--help`");
        }
    }
}
