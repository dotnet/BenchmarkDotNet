namespace BenchmarkDotNet.Running
{
    public enum IterationMode
    {
        /// <summary>
        /// <seealso cref="https://en.wikipedia.org/wiki/Pilot_experiment"/>
        /// </summary>
        Pilot,

        /// <summary>
        /// Warmup for idle method (overhead)
        /// </summary>
        WarmupIdle,

        /// <summary>
        /// Idle method (overhead)
        /// </summary>
        TargetIdle,

        /// <summary>
        /// Warmup for main benchmark iteration (with overhead)
        /// </summary>
        Warmup,

        /// <summary>
        /// Main benchmark iteration (with overhead)
        /// </summary>
        Target,

        /// <summary>
        /// Target - TargetIdle (without overhead)
        /// </summary>
        Result,

        /// <summary>
        /// Unknown 
        /// </summary>
        Unknown
    }
}