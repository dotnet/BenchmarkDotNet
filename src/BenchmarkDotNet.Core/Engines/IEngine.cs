using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public interface IEngine
    {
        [NotNull]
        Job TargetJob { get; }

        long OperationsPerInvoke { get; }

        [CanBeNull]
        Action SetupAction { get; }

        [CanBeNull]
        Action CleanupAction { get; }

        [NotNull]
        Action<long> MainAction { get; }

        [NotNull]
        Action<long> IdleAction { get; }

        bool IsDiagnoserAttached { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        void WriteLine();
        void WriteLine(string line);

        RunResults Run();

        /// <summary>
        /// this type will be used in the auto-generated program to create engine in separate process
        /// <remarks>it must have parameterless constructor</remarks>
        /// </summary>
        IEngineFactory Factory { get; }
    }
}