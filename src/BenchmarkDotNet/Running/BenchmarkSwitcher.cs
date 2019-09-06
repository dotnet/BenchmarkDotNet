using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
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
        [PublicAPI] public IEnumerable<Summary> RunAll(IConfig config = null) => Run(new[] { "--filter", "*" }, config);

        /// <summary>
        /// Run all available benchmarks and join them to a single summary
        /// </summary>
        [PublicAPI] public Summary RunAllJoined(IConfig config = null) => Run(new[] { "--filter", "*", "--join" }, config).Single();

        [PublicAPI]
        public IEnumerable<Summary> Run(string[] args = null, IConfig config = null)
        {
            // VS generates bad assembly binding redirects for ValueTuple for Full .NET Framework 
            // we need to keep the logic that uses it in a separate method and create DirtyAssemblyResolveHelper first
            // so it can ignore the version mismatch ;)
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithDirtyAssemblyResolveHelper(args ?? Array.Empty<string>(), config ?? DefaultConfig.Instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IEnumerable<Summary> RunWithDirtyAssemblyResolveHelper(string[] args, IConfig config)
        {
            var logger = config.GetNonNullCompositeLogger();
            var (isParsingSuccess, parsedConfig, options) = ConfigParser.Parse(args, logger, config);
            if (!isParsingSuccess) // invalid console args, the ConfigParser printed the error
                return Array.Empty<Summary>();

            if (options.PrintInformation)
            {
                logger.WriteLine(HostEnvironmentInfo.GetInformation());
                return Array.Empty<Summary>();
            }

            var effectiveConfig = ManualConfig.Union(config, parsedConfig);

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

            var benchmarksToFilter = options.UserProvidedFilters
                ? allAvailableTypesWithRunnableBenchmarks
                : userInteraction.AskUser(allAvailableTypesWithRunnableBenchmarks, logger);

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
    }
}