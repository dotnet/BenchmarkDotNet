using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided glob patterns
    /// </summary>
    public class GlobFilter : IFilter
    {
        private readonly Regex[] patterns;

        public GlobFilter(string[] patterns)  => this.patterns = patterns.Select(pattern => new Regex(WildcardToRegex(pattern))).ToArray();

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var benchmark = benchmarkCase.Descriptor.WorkloadMethod;
            var fullBenchmarkName = $"{benchmark.DeclaringType.GetCorrectCSharpTypeName(includeGenericArgumentsNamespace: false)}.{benchmark.Name}";

            return patterns.Any(pattern => pattern.IsMatch(fullBenchmarkName));
        }

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}