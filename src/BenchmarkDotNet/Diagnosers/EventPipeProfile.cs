namespace BenchmarkDotNet.Diagnosers
{
    public enum EventPipeProfile
    {
        /// <summary>
        /// Useful for tracking CPU usage and general .NET runtime information.
        /// This is the default option if no profile or providers are specified.
        /// </summary>
        CpuSampling,
        /// <summary>
        /// Tracks GC collections and samples object allocations.
        /// </summary>
        GcVerbose,
        /// <summary>
        /// Tracks GC collections only at very low overhead.
        /// </summary>
        GcCollect,
        /// <summary>
        /// Logging when Just in time (JIT) compilation occurs.
        /// Logging of the internal workings of the Just In Time compiler. This is fairly verbose.
        /// It details decisions about interesting optimization (like inlining and tail call)
        /// </summary>
        Jit
    }
}