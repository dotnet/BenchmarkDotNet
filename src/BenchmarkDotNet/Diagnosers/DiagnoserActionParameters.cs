using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserActionParameters
    {
        public DiagnoserActionParameters(Process process, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId)
        {
            Process = process;
            BenchmarkCase = benchmarkCase;
            BenchmarkId = benchmarkId;
        }

        public DiagnoserActionParameters(Process process, ExecuteParameters executeParameters)
            : this(process, executeParameters.BenchmarkCase, executeParameters.BenchmarkId)
        {
        }

        public Process Process { get; }

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        public ImmutableConfig Config => BenchmarkCase.Config;
    }
}