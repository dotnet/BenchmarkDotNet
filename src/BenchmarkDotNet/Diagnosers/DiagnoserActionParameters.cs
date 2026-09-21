using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Diagnostics;

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

        public Process? Process { get; }

        public int ProcessId => Process?.Id ?? throw new InvalidOperationException("The process instance is not set.");

        public BenchmarkCase BenchmarkCase { get; }

        public BenchmarkId BenchmarkId { get; }

        /// <summary>The runnable type an in-process toolchain emitted for this benchmark, which only it can name unambiguously.</summary>
        internal Type? InProcessRunnableType { get; init; }

        public ImmutableConfig Config => BenchmarkCase.Config;
    }
}