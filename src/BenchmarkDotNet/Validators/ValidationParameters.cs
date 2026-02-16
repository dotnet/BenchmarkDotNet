using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ValidationParameters
    {
        public IReadOnlyList<BenchmarkCase> Benchmarks { get; }

        public ImmutableConfig Config { get; }

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