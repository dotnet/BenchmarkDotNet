using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Properties;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkSwitcher
    {
        private readonly ConsoleLogger logger = new ConsoleLogger();
        private readonly TypeParser typeParser;

        public BenchmarkSwitcher(Type[] types)
        {
            foreach (var type in types.Where(type => !type.ContainsRunnableBenchmarks()).ToArray())
            {
                logger.WriteLineError($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed types with public instance [Benchmark] method(s) are supported.");
            }

            typeParser = new TypeParser(types.Where(type => type.ContainsRunnableBenchmarks()).ToArray(), logger);
        }

        public BenchmarkSwitcher(Assembly assembly)
        {
            typeParser = new TypeParser(assembly.GetRunnableBenchmarks(), logger);
        }

        public BenchmarkSwitcher(Assembly[] assemblies)
        {
            var runnableBenchmarkTypes = assemblies.SelectMany(a => a.GetRunnableBenchmarks()).ToArray();
            typeParser = new TypeParser(runnableBenchmarkTypes, logger);
        }

        public static BenchmarkSwitcher FromTypes(Type[] types) => new BenchmarkSwitcher(types);

        public static BenchmarkSwitcher FromAssembly(Assembly assembly) => new BenchmarkSwitcher(assembly);

        public static BenchmarkSwitcher FromAssemblies(Assembly[] assemblies) => new BenchmarkSwitcher(assemblies);

        public static BenchmarkSwitcher FromAssemblyAndTypes(Assembly assembly, Type[] types) 
            => new BenchmarkSwitcher(assembly.GetRunnableBenchmarks().Concat(types).ToArray());

        /// <summary>
        /// Run all available benchmarks.
        /// </summary>
        public IEnumerable<Summary> RunAll() => Run(new[] { "*" });

        /// <summary>
        /// Run all available benchmarks and join them to a single summary
        /// </summary>
        public Summary RunAllJoined() => Run(new[] { "*", "--join" }).Single();

        public IEnumerable<Summary> Run(string[] args = null, IConfig config = null)
        {
            args = args ?? Array.Empty<string>();

            if (ShouldDisplayOptions(args))
            {
                DisplayOptions();
                return Enumerable.Empty<Summary>();
            }

            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();

            var effectiveConfig = ManualConfig.Union(config ?? DefaultConfig.Instance, ManualConfig.Parse(args));
            bool join = args.Any(arg => arg.EqualsWithIgnoreCase("--join"));

            var benchmarks = Filter(effectiveConfig);

            summaries.AddRange(BenchmarkRunner.Run(benchmarks, effectiveConfig, summaryPerType: !join));

            var totalNumberOfExecutedBenchmarks = summaries.Sum(summary => summary.GetNumberOfExecutedBenchmarks());
            BenchmarkRunner.LogTotalTime(logger, globalChronometer.GetElapsed().GetTimeSpan(), totalNumberOfExecutedBenchmarks, "Global total time");
            return summaries;
        }

        public bool ShouldDisplayOptions(string[] args) 
            => args.Any(arg => arg.EqualsWithIgnoreCase("--help") || arg.EqualsWithIgnoreCase("-h"));

        internal BenchmarkRunInfo[] Filter(IConfig effectiveConfig)
            => (effectiveConfig.GetFilters().Any() ? typeParser.GetAll() : typeParser.AskUser()) // if user provided some filters via args or custom config , we don't ask for any input
                .Select(typeWithMethods =>
                    typeWithMethods.AllMethodsInType
                        ? BenchmarkConverter.TypeToBenchmarks(typeWithMethods.Type, effectiveConfig)
                        : BenchmarkConverter.MethodsToBenchmarks(typeWithMethods.Type, typeWithMethods.Methods, effectiveConfig))
                .ToArray();

        private void DisplayOptions()
        {
            logger.WriteLineHeader($"{BenchmarkDotNetInfo.FullTitle}");
            logger.WriteLine();
            logger.WriteLineHeader("Options:");

            var consoleWidth = 80;
            try
            {
                consoleWidth = Console.WindowWidth;
            }
            // IOException causes build error
            // The type 'IOException' exists in both 
            //    'System.IO, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' and 
            //    'System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
            // TODO see if this error goes away after RC2, in the meantime just use "catch (Exception)"
            catch (Exception)
            {
                logger.WriteLine($"Unable to get the Console width, defaulting to {consoleWidth}");
            }

            ManualConfig.PrintOptions(logger, prefixWidth: 30, outputWidth: consoleWidth);
            logger.WriteLine();
            typeParser.PrintOptions(prefixWidth: 30, outputWidth: consoleWidth);
        }
    }
}