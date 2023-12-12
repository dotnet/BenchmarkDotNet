using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using System;
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

            if (assembly.IsDebug() ?? false)
                return Array.Empty<BenchmarkRunInfo>();

            return GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks())
                .Select(type => BenchmarkConverter.TypeToBenchmarks(type))
                .Where(runInfo => runInfo.BenchmarksCases.Length > 0)
                .ToArray();
        }
    }
}
