using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IEngine : IDisposable
    {
        IHost Host { get; }

        void WriteLine();

        void WriteLine(string line);

        Job TargetJob { get; }

        long OperationsPerInvoke { get; }

        Func<ValueTask> GlobalSetupAction { get; }

        Func<ValueTask> GlobalCleanupAction { get; }

        Func<long, IClock, ValueTask<ClockSpan>> WorkloadAction { get; }

        Func<long, IClock, ValueTask<ClockSpan>> OverheadAction { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        RunResults Run();
    }
}