using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;

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
                    return (false, Array.Empty<Type>());
                }
                else if (assemblies.Any())
                {
                    foreach (var assembly in assemblies)
                    {
                        logger.WriteLineError($"No [Benchmark] attribute found on '{assembly.GetName().Name}' assembly.");
                    }
                    return (false, Array.Empty<Type>());
                }
                logger.WriteLineError("No benchmarks were found.");
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
            => types
                .Select(type => BenchmarkConverter.TypeToBenchmarks(type, effectiveConfig))
                .Where(info => info.BenchmarksCases.Any())
                .ToArray();
    }
}