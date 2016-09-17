namespace BenchmarkDotNet.Engines
{
    public enum RunStrategy
    {
        /// <summary>
        /// A mode without overhead evaluating and warmup, with single invocation.
        /// </summary>
        ColdStart,

        /// <summary>
        /// Throughput mode.
        /// </summary>
        Throughput
    }
}