using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IEngine : IDisposable
    {
        [NotNull]
        IHost Host { get; }

        void WriteLine();

        void WriteLine(string line);

        [NotNull]
        Job TargetJob { get; }

        long OperationsPerInvoke { get; }

        [CanBeNull]
        Func<ValueTask> GlobalSetupAction { get; }

        [CanBeNull]
        Func<ValueTask> GlobalCleanupAction { get; }

        [NotNull]
        Func<long, IClock, ValueTask<ClockSpan>> WorkloadAction { get; }

        [NotNull]
        Func<long, IClock, ValueTask<ClockSpan>> OverheadAction { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        RunResults Run();
    }
}