using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class used for enumerating all the benchmarks in a source.
    /// </summary>
    internal static class BenchmarkEnumerator
    {
        /// <summary>
        /// Returns all the BenchmarkRunInfo objects from a given source.
        /// </summary>
        /// <param name="source">The dll or exe of the benchmark project.</param>
        /// <returns>The benchmarks inside the source.</returns>
        public static BenchmarkRunInfo[] GetBenchmarksFromSource(string source)
        {
            var assembly = Assembly.LoadFrom(source);

            // TODO: Allow for defining a base config inside the BDN project that is used by the VSTest Adapter.
            return GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks())
                .Select(type => BenchmarkConverter.TypeToBenchmarks(type))
                .Where(runInfo => runInfo.BenchmarksCases.Length > 0)
                .ToArray();
        }
    }
}
