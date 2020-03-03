namespace BenchmarkDotNet.Diagnosers
{
    /// <summary>
    /// Contains defined profiles for <see cref="EventPipeProfiler"/>.
    /// </summary>
    public enum EventPipeProfile
    {
        /// <summary>
        /// Useful for tracking CPU usage and general .NET runtime information. This is the default option if no profile or providers are specified.
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
    }
}