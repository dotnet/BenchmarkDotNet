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
        IHost Host { get; }

        bool IsDiagnoserAttached { get; }

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
        Action<long> MainAction { get; }

        [NotNull]
        Action<long> IdleAction { get; }

        IResolver Resolver { get; }

        Measurement RunIteration(IterationData data);

        /// <summary>
        /// must provoke all static ctors and perform any other necessary allocations 
        /// so Run() has 0 exclusive allocations and our Memory Diagnostics is 100% accurate!
        /// </summary>
        void PreAllocate();

        /// <summary>
        /// must perform jitting via warmup calls
        /// <remarks>is called after first call to GlobalSetup, from the auto-generated benchmark process</remarks>
        /// </summary>
        void Jitting();

        RunResults Run();
    }
}