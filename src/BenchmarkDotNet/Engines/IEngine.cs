using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

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

        Action? GlobalSetupAction { get; }

        Action? GlobalCleanupAction { get; }

        Action<long> WorkloadAction { get; }

        Action<long> OverheadAction { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        RunResults Run();
    }
}