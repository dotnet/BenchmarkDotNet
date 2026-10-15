using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ValidationParameters
    {
        public IReadOnlyList<BenchmarkCase> Benchmarks { get; }

        public ImmutableConfig Config { get; }

        /// <summary>
        /// One descriptor per benchmark type: validators read the type from <see cref="Descriptor.Type"/>, whose annotation tells
        /// the trimmer to keep its members, which a <see cref="Type"/> passed through LINQ would lose.
        /// </summary>
        internal IEnumerable<Descriptor> DistinctBenchmarkTypes()
            => Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type).Select(group => group.First().Descriptor);

        public ValidationParameters(IReadOnlyList<BenchmarkCase> benchmarks, ImmutableConfig config)
        {
            Benchmarks = benchmarks;
            Config = config;
        }

        // Note: Following implicit operators are expected to be used for test projects.
        public static implicit operator ValidationParameters(BenchmarkCase[] benchmarksCase) => new ValidationParameters(benchmarksCase, DefaultConfig.Instance.CreateImmutableConfig());
        public static implicit operator ValidationParameters(BenchmarkRunInfo benchmarkRunInfo) => new ValidationParameters(benchmarkRunInfo.BenchmarksCases, benchmarkRunInfo.Config);
    }
}