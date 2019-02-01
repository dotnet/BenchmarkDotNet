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

        // to have backward compatibility for people who implemented IValidator(Benchmark[] benchmarks)
        public static implicit operator ValidationParameters(BenchmarkCase[] benchmarksCase) => new ValidationParameters(benchmarksCase, null);
        public static implicit operator ValidationParameters(BenchmarkRunInfo benchmarkRunInfo) => new ValidationParameters(benchmarkRunInfo.BenchmarksCases, null);
    }
}