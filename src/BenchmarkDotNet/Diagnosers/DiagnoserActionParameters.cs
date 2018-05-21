using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserActionParameters
    {
        public DiagnoserActionParameters(Process process, Benchmark benchmark, BenchmarkId benchmarkId, IConfig config)
        {
            Process = process;
            Benchmark = benchmark;
            BenchmarkId = benchmarkId;
            Config = config;
        }

        public Process Process { get; }

        public Benchmark Benchmark { get; }

        public BenchmarkId BenchmarkId { get; }

        public IConfig Config { get; }
    }
}