namespace BenchmarkDotNet.Engines
{
    public enum RunStrategy
    {
        /// <summary>
        /// Throughput mode.
        /// Perfect for microbenchmarking.
        /// </summary>
        Throughput,

        /// <summary>
        /// A mode without overhead evaluating and warmup, with single invocation.
        /// Perfect for startup time evaluation.
        /// </summary>
        ColdStart,

        /// <summary>
        /// A mode without overhead evaluating, with several target iterations.
        /// Perfect for macrobenchmarks without a steady state with high variance.
        /// </summary>
        Monitoring
    }
}