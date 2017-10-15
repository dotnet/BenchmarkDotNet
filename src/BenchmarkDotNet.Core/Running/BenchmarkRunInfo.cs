using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo
    {
        public BenchmarkRunInfo(Benchmark[] benchmarks, Type type, ReadOnlyConfig config)
        {
            Benchmarks = benchmarks;
            Type = type;
            Config = config;
        }
        public Benchmark[] Benchmarks { get; }
        public Type Type { get; }
        public ReadOnlyConfig Config { get; }
    }
}