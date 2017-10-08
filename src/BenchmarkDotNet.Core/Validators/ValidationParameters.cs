using System.Collections.Generic;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class ValidationParameters
    {
        public IReadOnlyList<Benchmark> Benchmarks { get; }

        public IConfig Config { get; }

        public ValidationParameters(IReadOnlyList<Benchmark> benchmarks, IConfig config)
        {
            Benchmarks = benchmarks;
            Config = config;
        }

        // to have backward compatibility for people who implemented IValidator(Benchmark[] benchmarks)
        public static implicit operator ValidationParameters(Benchmark[] benchmarks) => new ValidationParameters(benchmarks, null);
        public static implicit operator ValidationParameters(BenchmarkRunInfo benchmarkRunInfo) => new ValidationParameters(benchmarkRunInfo.Benchmarks, null);
    }
}