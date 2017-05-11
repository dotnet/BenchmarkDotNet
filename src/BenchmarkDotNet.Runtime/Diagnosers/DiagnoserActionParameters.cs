using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserActionParameters
    {
        public DiagnoserActionParameters(Process process, Benchmark benchmark, IConfig config)
        {
            Process = process;
            Benchmark = benchmark;
            Config = config;
        }

        public Process Process { get; }

        public Benchmark Benchmark { get; }

        public IConfig Config { get; }
    }
}