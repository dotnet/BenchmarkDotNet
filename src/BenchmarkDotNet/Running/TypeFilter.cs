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

            bool hasRunnableTypeBenchmarks = types.Any(type => type.ContainsRunnableBenchmarks());
            bool hasRunnableAssemblyBenchmarks = assemblies.Any(assembly => GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks()).Length > 0);

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

            foreach (var type in types)
            {
                if (type.ContainsRunnableBenchmarks())
                {
                    validRunnableTypes.AddRange(GenericBenchmarksBuilder.BuildGenericsIfNeeded(type).Where(tuple => tuple.isSuccess).Select(tuple => tuple.result));
                }
                else
                {
                    logger.WriteLineError($"Type {type} is invalid. Only public, non-generic (closed generic types with public parameterless ctors are supported), non-abstract, non-sealed, non-static types with public instance [Benchmark] method(s) are supported.");

                    return (false, Array.Empty<Type>());
                }
            }

            foreach (var assembly in assemblies)
            {
                validRunnableTypes.AddRange(GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks()));
            }

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