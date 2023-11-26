using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserActionParameters
    {
        public DiagnoserActionParameters(Process? process, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId)
        {
            Process = process;
            BenchmarkCase = benchmarkCase;
            BenchmarkId = benchmarkId;
        }

        public Process Process { get; }

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        public ImmutableConfig Config => BenchmarkCase.Config;
    }
}