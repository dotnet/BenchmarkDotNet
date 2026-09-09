using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using System.Reflection;

namespace BenchmarkDotNet.Running
{
    public static class TypeFilter
    {
        public static (bool allTypesValid, IReadOnlyList<Type> runnable) GetTypesWithRunnableBenchmarks(IEnumerable<Type> types, IEnumerable<Assembly> assemblies, ILogger logger)
        {
            var validRunnableTypes = new List<Type>();

            // Built once: the guard below and the list at the end both need it.
            var assemblyTypes = assemblies
                .SelectMany(assembly => GenericBenchmarksBuilder.BuildRunnableBenchmarks(assembly.GetRunnableBenchmarks()))
                .ToArray();

            bool hasRunnableTypeBenchmarks = types.Any(type => type.ContainsRunnableBenchmarks());

            // A type whose attributes cannot be read counts as declaring benchmarks here - it does, and telling the
            // user that no [Benchmark] was found would be wrong. It is reported below, in place of being run.
            bool hasRunnableAssemblyBenchmarks = assemblyTypes.Any(built => built.IsSuccess || built.IsUnreadable);

            if (!hasRunnableTypeBenchmarks && !hasRunnableAssemblyBenchmarks)
            {
                if (types.Any())
                {
                    foreach (var type in types)
                    {
                        logger.WriteLineError($"No [Benchmark] attribute found on '{type.Name}' benchmark case.");
                    }
                }
                else if (assemblies.Any())
                {
                    foreach (var assembly in assemblies)
                    {
                        logger.WriteLineError($"No [Benchmark] attribute found on '{assembly.GetName().Name}' assembly.");
                    }
                }
                else
                {
                    logger.WriteLineError("No benchmarks were found.");
                }
                return (false, Array.Empty<Type>());
            }

            // A type whose attributes cannot be read at all - [Config(typeof(SomeAbstractConfig))], say, where the
            // attribute throws while reflection constructs it - is dropped rather than allowed to abort the run, but
            // that has to be said out loud. GenericBenchmarksValidator says so too, and on the paths that never come
            // through here it is the only one that can, but it only runs once at least one benchmark survived: when
            // the unreadable type was the only one, this is the one place left to say why nothing was found. Saying
            // it twice on the way to a run is the lesser problem. A type that failed on its [GenericTypeArguments]
            // is left to the validator alone, which is where that has always been reported.
            void AddRunnable(IEnumerable<GenericBenchmarkType> built)
            {
                foreach (var candidate in built)
                {
                    if (candidate.IsSuccess)
                        validRunnableTypes.Add(candidate.Type);
                    else if (candidate.IsUnreadable)
                        logger.WriteLineError(candidate.Error!);
                }
            }

            foreach (var type in types)
            {
                if (type.ContainsRunnableBenchmarks())
                {
                    AddRunnable(GenericBenchmarksBuilder.BuildGenericsIfNeeded(type));
                }
                else
                {
                    logger.WriteLineError($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");

                    return (false, Array.Empty<Type>());
                }
            }

            AddRunnable(assemblyTypes);

            return (true, validRunnableTypes);
        }

        public static BenchmarkRunInfo[] Filter(IConfig effectiveConfig, IEnumerable<Type> types)
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            return context.ExecuteUntilComplete(FilterAsync(effectiveConfig, types, CancellationToken.None));
        }

        public static async ValueTask<BenchmarkRunInfo[]> FilterAsync(IConfig effectiveConfig, IEnumerable<Type> types, CancellationToken cancellationToken)
        {
            var result = new List<BenchmarkRunInfo>();
            foreach (var type in types)
            {
                var info = await BenchmarkConverter.TypeToBenchmarksAsync(type, effectiveConfig, cancellationToken).ConfigureAwait();
                if (info.BenchmarksCases.Any())
                    result.Add(info);
            }
            return result.ToArray();
        }
    }
}