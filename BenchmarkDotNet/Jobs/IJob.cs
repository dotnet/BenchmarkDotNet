using System;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Jobs
{
    public interface IJob : IEquatable<IJob>
    {
        IToolchain Toolchain { get; }
        Mode Mode { get; }
        Platform Platform { get; }
        Jit Jit { get; }
        Framework Framework { get; }
        Runtime Runtime { get; }
        Count ProcessCount { get; }
        Count WarmupCount { get; }
        Count TargetCount { get; }

        /// <summary>
        /// ProcessorAffinity for the benchmark process.
        /// <seealso cref="https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx"/>
        /// </summary>
        Count Affinity { get; }
    }
}