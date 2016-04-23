using System;

namespace BenchmarkDotNet.Jobs
{
    public interface IJob : IEquatable<IJob>
    {
        Mode Mode { get; }
        Platform Platform { get; }
        Jit Jit { get; }
        Framework Framework { get; }
        Runtime Runtime { get; }
        Count LaunchCount { get; }
        Count WarmupCount { get; }
        Count TargetCount { get; }

        /// <summary>
        /// Desired time of execution of an iteration (in ms).
        /// </summary>
        Count IterationTime { get; }

        /// <summary>
        /// ProcessorAffinity for the benchmark process.
        /// <seealso href="https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx"/>
        /// </summary>
        Count Affinity { get; }

        Property[] AllProperties { get; }
    }
}