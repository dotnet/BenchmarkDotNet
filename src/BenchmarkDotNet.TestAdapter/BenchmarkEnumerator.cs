using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class used for enumerating all the benchmarks in an assembly.
    /// </summary>
    internal static class BenchmarkEnumerator
    {
        /// <summary>
        /// Returns all the BenchmarkRunInfo objects from a given assembly.
        /// </summary>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <returns>The benchmarks inside the assembly.</returns>
        public static BenchmarkRunInfo[] GetBenchmarksFromAssemblyPath(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);

            var isDebugAssembly = assembly.IsJitOptimizationDisabled() ?? false;

            return GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks())
                .Select(type =>
                {
                    var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(type);
                    if (isDebugAssembly)
                    {
                        // If the assembly is a debug assembly, then only display them if they will run in-process
                        // This will allow people to debug their benchmarks using VSTest if they wish.
                        benchmarkRunInfo = new BenchmarkRunInfo(
                            benchmarkRunInfo.BenchmarksCases.Where(c => c.GetToolchain().IsInProcess).ToArray(),
                            benchmarkRunInfo.Type,
                            benchmarkRunInfo.Config);
                    }

                    return benchmarkRunInfo;
                })
                .Where(runInfo => runInfo.BenchmarksCases.Length > 0)
                .ToArray();
        }
    }
}
