using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided glob patterns
    /// </summary>
    public class GlobFilter : IFilter
    {
        private readonly (string userValue, Regex regex)[] patterns;

        public GlobFilter(string[] patterns)  => this.patterns = patterns.Select(pattern => (pattern, new Regex(WildcardToRegex(pattern)))).ToArray();

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var benchmark = benchmarkCase.Descriptor.WorkloadMethod;
            string fullBenchmarkName = benchmarkCase.Descriptor.GetFilterName();
            string typeName = benchmark.DeclaringType.GetDisplayName();

            return patterns.Any(pattern => typeName.EqualsWithIgnoreCase(pattern.userValue) || pattern.regex.IsMatch(fullBenchmarkName));
        }

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}