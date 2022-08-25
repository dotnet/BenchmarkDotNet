using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

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
        Action GlobalSetupAction { get; }

        [CanBeNull]
        Action GlobalCleanupAction { get; }

        [NotNull]
        Action<long> WorkloadAction { get; }

        [NotNull]
        Action<long> OverheadAction { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        RunResults Run();
    }
}