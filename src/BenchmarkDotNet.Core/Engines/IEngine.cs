using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public interface IEngine
    {
        [NotNull]
        Job TargetJob { get; set; }

        long OperationsPerInvoke { get; set; }

        [CanBeNull]
        Action SetupAction { get; set; }

        [CanBeNull]
        Action CleanupAction { get; set; }

        [NotNull]
        Action<long> MainAction { get; }

        [NotNull]
        Action<long> IdleAction { get; }

        bool IsDiagnoserAttached { get; set; }

        Measurement RunIteration(IterationData data);

        void WriteLine();
        void WriteLine(string line);

        IResolver Resolver { get; }
    }
}