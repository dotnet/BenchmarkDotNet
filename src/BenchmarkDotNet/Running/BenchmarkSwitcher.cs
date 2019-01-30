using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Properties;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkSwitcher
    {
        private readonly IUserInteraction userInteraction = new UserInteraction();
        private readonly List<Type> types = new List<Type>();
        private readonly List<Assembly> assemblies = new List<Assembly>();

        internal BenchmarkSwitcher(IUserInteraction userInteraction) => this.userInteraction = userInteraction;

        [PublicAPI] public BenchmarkSwitcher(Type[] types) => this.types.AddRange(types);

        [PublicAPI] public BenchmarkSwitcher(Assembly assembly) => assemblies.Add(assembly);

        [PublicAPI] public BenchmarkSwitcher(Assembly[] assemblies) => this.assemblies.AddRange(assemblies);
        
        [PublicAPI] public BenchmarkSwitcher With(Type type) { types.Add(type); return this; }

        [PublicAPI] public BenchmarkSwitcher With(Type[] types) { this.types.AddRange(types); return this; }

        [PublicAPI] public BenchmarkSwitcher With(Assembly assembly) { assemblies.Add(assembly); return this; }

        [PublicAPI] public BenchmarkSwitcher With(Assembly[] assemblies) { this.assemblies.AddRange(assemblies); return this; }

        [PublicAPI] public static BenchmarkSwitcher FromTypes(Type[] types) => new BenchmarkSwitcher(types);

        [PublicAPI] public static BenchmarkSwitcher FromAssembly(Assembly assembly) => new BenchmarkSwitcher(assembly);

        [PublicAPI] public static BenchmarkSwitcher FromAssemblies(Assembly[] assemblies) => new BenchmarkSwitcher(assemblies);

        /// <summary>
        /// Run all available benchmarks.
        /// </summary>
        [PublicAPI] public IEnumerable<Summary> RunAll() => Run(new[] { "--filter", "*" });

        /// <summary>
        /// Run all available benchmarks and join them to a single summary
        /// </summary>
        [PublicAPI] public Summary RunAllJoined() => Run(new[] { "--filter", "*", "--join" }).Single();


        [PublicAPI] public IEnumerable<Summary> Run(string[] args = null, IConfig config = null)
        {
            var nonNullConfig = config ?? DefaultConfig.Instance;
            // if user did not provide any loggers, we use the ConsoleLogger to somehow show the errors to the user
            var nonNullLogger = nonNullConfig.GetLoggers().Any() ? nonNullConfig.GetCompositeLogger() : ConsoleLogger.Default;

            OptionHandler optionHandler = new OptionHandler();
            var result = CommandLineParser.Parser(
                optionHandler, 
                args, 
                nonNullLogger,
                $"{BenchmarkDotNetInfo.FullTitle} - Powerful .NET library for benchmarking.");

            if (result != 0 || optionHandler.Options == null)
            {
                return Array.Empty<Summary>();
            }

            return Run(optionHandler.Options, nonNullConfig, nonNullLogger);
        }

        internal IEnumerable<Summary> Run(CommandLineOptions options, IConfig config, ILogger logger)
        { 
            var (isParsingSuccess, parsedConfig) = ConfigParser.Parse(options, logger, config);
            if (!isParsingSuccess) // invalid console args, the ConfigParser printed the error
                return Array.Empty<Summary>();

            if (options.PrintInformation)
            {
                logger.WriteLine(HostEnvironmentInfo.GetInformation());
                return Array.Empty<Summary>();
            }

            var effectiveConfig = ManualConfig.Union(config, parsedConfig);

            (var allTypesValid, var allAvailableTypesWithRunnableBenchmarks) = TypeFilter.GetTypesWithRunnableBenchmarks(types, assemblies, logger);
            if (!allTypesValid) // there were some invalid and TypeFilter printed errors
                return Array.Empty<Summary>();
            
            if (allAvailableTypesWithRunnableBenchmarks.IsEmpty())
            {
                userInteraction.PrintNoBenchmarksError(logger);
                return Array.Empty<Summary>();
            }

            if (options.ListBenchmarkCaseMode != ListBenchmarkCaseMode.Disabled)
            {
                PrintList(logger, effectiveConfig, allAvailableTypesWithRunnableBenchmarks, options);
                return Array.Empty<Summary>();
            }
            
            var benchmarksToFilter = options.UserProvidedFilters
                ? allAvailableTypesWithRunnableBenchmarks 
                : userInteraction.AskUser(allAvailableTypesWithRunnableBenchmarks, logger);

            var filteredBenchmarks = TypeFilter.Filter(effectiveConfig, benchmarksToFilter);

            if (filteredBenchmarks.IsEmpty())
            {
                userInteraction.PrintWrongFilterInfo(benchmarksToFilter, logger, options.Filters.ToArray());
                return Array.Empty<Summary>();
            }

            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();

            summaries.AddRange(BenchmarkRunner.Run(filteredBenchmarks, effectiveConfig));

            int totalNumberOfExecutedBenchmarks = summaries.Sum(summary => summary.GetNumberOfExecutedBenchmarks());
            BenchmarkRunner.LogTotalTime(logger, globalChronometer.GetElapsed().GetTimeSpan(), totalNumberOfExecutedBenchmarks, "Global total time");
            return summaries;
        }

        private static void PrintList(ILogger nonNullLogger, IConfig effectiveConfig, IReadOnlyList<Type> allAvailableTypesWithRunnableBenchmarks, CommandLineOptions options)
        {
            var printer = new BenchmarkCasesPrinter(options.ListBenchmarkCaseMode);
            
            var testNames = TypeFilter.Filter(effectiveConfig, allAvailableTypesWithRunnableBenchmarks)
                .SelectMany(p => p.BenchmarksCases)
                .Select(p => p.Descriptor.GetFilterName())
                .Distinct();

            printer.Print(testNames, nonNullLogger);
        }
    }
}