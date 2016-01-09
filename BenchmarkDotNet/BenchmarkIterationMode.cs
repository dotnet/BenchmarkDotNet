namespace BenchmarkDotNet
{
    public enum BenchmarkIterationMode
    {
        /// <summary>
        /// <seealso cref="https://en.wikipedia.org/wiki/Pilot_experiment"/>
        /// </summary>
        Pilot,
        WarmupIdle,
        TargetIdle,
        Warmup,
        Target
    }
}