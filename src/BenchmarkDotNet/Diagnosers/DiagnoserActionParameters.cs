using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserActionParameters
    {
        public DiagnoserActionParameters(Process process, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IConfig config)
        {
            Process = process;
            BenchmarkCase = benchmarkCase;
            BenchmarkId = benchmarkId;
            Config = config;
        }

        public Process Process { get; }

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        public IConfig Config { get; }
    }
}