using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkSwitcher
    {
        private readonly ConsoleLogger logger = new ConsoleLogger();
        private readonly TypeParser typeParser;

        [PublicAPI] public BenchmarkSwitcher(Type[] types)
        {
            foreach (var type in types.Where(type => !type.ContainsRunnableBenchmarks()).ToArray())
            {
                logger.WriteLineError($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed types with public instance [Benchmark] method(s) are supported.");
            }

            typeParser = new TypeParser(types.Where(type => type.ContainsRunnableBenchmarks()).ToArray(), logger);
        }

        [PublicAPI] public BenchmarkSwitcher(Assembly assembly)
        {
            typeParser = new TypeParser(assembly.GetRunnableBenchmarks(), logger);
        }

        [PublicAPI] public BenchmarkSwitcher(Assembly[] assemblies)
        {
            var runnableBenchmarkTypes = assemblies.SelectMany(a => a.GetRunnableBenchmarks()).ToArray();
            typeParser = new TypeParser(runnableBenchmarkTypes, logger);
        }

        [PublicAPI] public static BenchmarkSwitcher FromTypes(Type[] types) => new BenchmarkSwitcher(types);

        [PublicAPI] public static BenchmarkSwitcher FromAssembly(Assembly assembly) => new BenchmarkSwitcher(assembly);

        [PublicAPI] public static BenchmarkSwitcher FromAssemblies(Assembly[] assemblies) => new BenchmarkSwitcher(assemblies);

        [PublicAPI] public static BenchmarkSwitcher FromAssemblyAndTypes(Assembly assembly, Type[] types) 
            => new BenchmarkSwitcher(assembly.GetRunnableBenchmarks().Concat(types).ToArray());

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
            args = args ?? Array.Empty<string>();

            var (isParsingSuccess, parsedConfig, options) = ConfigParser.Parse(args, ConsoleLogger.Default, config);
            if (!isParsingSuccess)
                return Enumerable.Empty<Summary>();

            if (options.PrintInformation)
            {
                Console.WriteLine(HostEnvironmentInfo.GetInformation());
                return Enumerable.Empty<Summary>();
            }

            var globalChronometer = Chronometer.Start();
            var summaries = new List<Summary>();

            var effectiveConfig = ManualConfig.Union(config ?? DefaultConfig.Instance, parsedConfig);

            var filteredBenchmarks = typeParser.Filter(effectiveConfig);
            if (filteredBenchmarks.IsEmpty())
                return Array.Empty<Summary>();

            summaries.AddRange(BenchmarkRunner.Run(filteredBenchmarks, effectiveConfig));

            int totalNumberOfExecutedBenchmarks = summaries.Sum(summary => summary.GetNumberOfExecutedBenchmarks());
            BenchmarkRunner.LogTotalTime(logger, globalChronometer.GetElapsed().GetTimeSpan(), totalNumberOfExecutedBenchmarks, "Global total time");
            return summaries;
        }
    }
}