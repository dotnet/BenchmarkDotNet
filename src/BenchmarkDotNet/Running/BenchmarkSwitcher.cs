using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;

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

        [PublicAPI]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public BenchmarkSwitcher With(Type[] types) { this.types.AddRange(types); return this; }

        [PublicAPI] public BenchmarkSwitcher With(Assembly assembly) { assemblies.Add(assembly); return this; }

        [PublicAPI]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public BenchmarkSwitcher With(Assembly[] assemblies) { this.assemblies.AddRange(assemblies); return this; }

        [PublicAPI] public static BenchmarkSwitcher FromTypes(Type[] types) => new BenchmarkSwitcher(types);

        [PublicAPI] public static BenchmarkSwitcher FromAssembly(Assembly assembly) => new BenchmarkSwitcher(assembly);

        [PublicAPI] public static BenchmarkSwitcher FromAssemblies(Assembly[] assemblies) => new BenchmarkSwitcher(assemblies);

        /// <summary>
        /// Run all available benchmarks.
        /// </summary>
        [PublicAPI] public IEnumerable<Summary> RunAll(IConfig? config = null, string[]? args = null)
        {
            args ??= Array.Empty<string>();
            if (ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => options.Filters = new[] { "*" }))
                args = updatedArgs;

            return Run(args, config);
        }

        /// <summary>
        /// Run all available benchmarks and join them to a single summary
        /// </summary>
        [PublicAPI] public Summary RunAllJoined(IConfig? config = null, string[]? args = null)
        {
            args ??= Array.Empty<string>();
            if (ConfigParser.TryUpdateArgs(args, out var updatedArgs, options => (options.Join, options.Filters) = (true, new[] { "*" })))
                args = updatedArgs;

            return Run(args, config).Single();
        }

        [PublicAPI]
        public IEnumerable<Summary> Run(string[]? args = null, IConfig? config = null)
        {
            // VS generates bad assembly binding redirects for ValueTuple for Full .NET Framework
            // we need to keep the logic that uses it in a separate method and create DirtyAssemblyResolveHelper first
            // so it can ignore the version mismatch ;)
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(args, config, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal IEnumerable<Summary> RunWithDirtyAssemblyResolveHelper(string[]? args, IConfig? config, bool askUserForInput)
        {
            var notNullArgs = args ?? Array.Empty<string>();
            var notNullConfig = config ?? DefaultConfig.Instance;

            var logger = notNullConfig.GetNonNullCompositeLogger();
            var (isParsingSuccess, parsedConfig, options) = ConfigParser.Parse(notNullArgs, logger, notNullConfig);
            if (!isParsingSuccess) // invalid console args, the ConfigParser printed the error
                return Array.Empty<Summary>();

            if (args == null && Environment.GetCommandLineArgs().Length > 1) // The first element is the executable file name
                logger.WriteLineHint("You haven't passed command line arguments to BenchmarkSwitcher.Run method. Running with default configuration.");

            if (options.PrintInformation)
            {
                logger.WriteLine(HostEnvironmentInfo.GetInformation());
                return Array.Empty<Summary>();
            }

            var effectiveConfig = ManualConfig.Union(notNullConfig, parsedConfig);

            var (allTypesValid, allAvailableTypesWithRunnableBenchmarks) = TypeFilter.GetTypesWithRunnableBenchmarks(types, assemblies, logger);
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

            var benchmarksToFilter = options.UserProvidedFilters || !askUserForInput
                ? allAvailableTypesWithRunnableBenchmarks
                : userInteraction.AskUser(allAvailableTypesWithRunnableBenchmarks, logger);

            if (effectiveConfig.Options.HasFlag(ConfigOptions.ApplesToApples))
            {
                return ApplesToApples(ImmutableConfigBuilder.Create(effectiveConfig), benchmarksToFilter, logger, options);
            }

            var filteredBenchmarks = TypeFilter.Filter(effectiveConfig, benchmarksToFilter);

            if (filteredBenchmarks.IsEmpty())
            {
                userInteraction.PrintWrongFilterInfo(benchmarksToFilter, logger, options.Filters.ToArray());
                return Array.Empty<Summary>();
            }

            return BenchmarkRunnerClean.Run(filteredBenchmarks);
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

        private IEnumerable<Summary> ApplesToApples(ImmutableConfig effectiveConfig, IReadOnlyList<Type> benchmarksToFilter, ILogger logger, CommandLineOptions options)
        {
            var jobs = effectiveConfig.GetJobs().ToArray();
            if (jobs.Length <= 1)
            {
                logger.WriteError("To use apples-to-apples comparison you must specify at least two Job objects.");
                return Array.Empty<Summary>();
            }
            var baselineJob = jobs.SingleOrDefault(job => job.Meta.Baseline);
            if (baselineJob == default)
            {
                logger.WriteError("To use apples-to-apples comparison you must specify exactly ONE baseline Job object.");
                return Array.Empty<Summary>();
            }
            else if (jobs.Any(job => !job.Run.HasValue(RunMode.IterationCountCharacteristic)))
            {
                logger.WriteError("To use apples-to-apples comparison you must specify the number of iterations in explicit way.");
                return Array.Empty<Summary>();
            }

            Job invocationCountJob = baselineJob
                .WithWarmupCount(1)
                .WithIterationCount(1)
                .WithEvaluateOverhead(false);

            ManualConfig invocationCountConfig = ManualConfig.Create(effectiveConfig);
            invocationCountConfig.RemoveAllJobs();
            invocationCountConfig.RemoveAllDiagnosers();
            invocationCountConfig.AddJob(invocationCountJob);

            var invocationCountBenchmarks = TypeFilter.Filter(invocationCountConfig, benchmarksToFilter);
            if (invocationCountBenchmarks.IsEmpty())
            {
                userInteraction.PrintWrongFilterInfo(benchmarksToFilter, logger, options.Filters.ToArray());
                return Array.Empty<Summary>();
            }

            logger.WriteLineHeader("Each benchmark is going to be executed just once to get invocation counts.");
            Summary[] invocationCountSummaries = BenchmarkRunnerClean.Run(invocationCountBenchmarks);

            Dictionary<(Descriptor Descriptor, ParameterInstances Parameters), Measurement> dictionary = invocationCountSummaries
                .SelectMany(summary => summary.Reports)
                .ToDictionary(
                    report => (report.BenchmarkCase.Descriptor, report.BenchmarkCase.Parameters),
                    report => report.AllMeasurements.Single(measurement => measurement.IsWorkload() && measurement.IterationStage == Engines.IterationStage.Actual));

            int iterationCount = baselineJob.Run.IterationCount;
            BenchmarkRunInfo[] benchmarksWithoutInvocationCount = TypeFilter.Filter(effectiveConfig, benchmarksToFilter);
            BenchmarkRunInfo[] benchmarksWithInvocationCount = benchmarksWithoutInvocationCount
                .Select(benchmarkInfo => new BenchmarkRunInfo(
                    benchmarkInfo.BenchmarksCases.Select(benchmark =>
                        new BenchmarkCase(
                            benchmark.Descriptor,
                            benchmark.Job
                                .WithIterationCount(iterationCount)
                                .WithEvaluateOverhead(false)
                                .WithWarmupCount(1)
                                .WithOutlierMode(OutlierMode.DontRemove)
                                .WithInvocationCount(dictionary[(benchmark.Descriptor, benchmark.Parameters)].Operations)
                                .WithUnrollFactor(dictionary[(benchmark.Descriptor, benchmark.Parameters)].Operations % 16 == 0 ? 16 : 1),
                            benchmark.Parameters,
                            benchmark.Config)).ToArray(),
                    benchmarkInfo.Type, benchmarkInfo.Config))
                .ToArray();

            logger.WriteLineHeader("Actual benchmarking is going to happen now!");
            return BenchmarkRunnerClean.Run(benchmarksWithInvocationCount);
        }
    }
}