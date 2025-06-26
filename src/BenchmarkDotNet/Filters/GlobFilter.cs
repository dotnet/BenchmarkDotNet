using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided glob patterns
    /// </summary>
    public class GlobFilter : IFilter
    {
        private readonly Regex[] patterns;

        public GlobFilter(string[] patterns) => this.patterns = patterns.ToRegex();

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            var benchmark = benchmarkCase.Descriptor.WorkloadMethod;
            string nameWithoutArgs = benchmarkCase.Descriptor.GetFilterName();
            string fullBenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);

            return patterns.Any(pattern => pattern.IsMatch(fullBenchmarkName) || pattern.IsMatch(nameWithoutArgs));
        }
    }
}