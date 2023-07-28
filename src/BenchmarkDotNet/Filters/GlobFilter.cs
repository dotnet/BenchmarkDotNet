using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided glob patterns
    /// </summary>
    public class GlobFilter : IFilter
    {
        private readonly Regex[] patterns;

        public GlobFilter(string[] patterns) => this.patterns = ToRegex(patterns);

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var benchmark = benchmarkCase.Descriptor.WorkloadMethod;
            string nameWithoutArgs = benchmarkCase.Descriptor.GetFilterName();
            string fullBenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);

            return patterns.Any(pattern => pattern.IsMatch(fullBenchmarkName) || pattern.IsMatch(nameWithoutArgs));
        }

        internal static Regex[] ToRegex(string[] patterns)
            => patterns.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}