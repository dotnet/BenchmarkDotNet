namespace BenchmarkDotNet.Engines
{
    public enum IterationMode
    {
        /// <summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Pilot_experiment"/>
        /// </summary>
        Pilot,

        /// <summary>
        /// Warmup for idle method (overhead)
        /// </summary>
        IdleWarmup,

        /// <summary>
        /// Idle method (overhead)
        /// </summary>
        IdleTarget,

        /// <summary>
        /// Warmup for main benchmark iteration (with overhead)
        /// </summary>
        MainWarmup,

        /// <summary>
        /// Main benchmark iteration (with overhead)
        /// </summary>
        MainTarget,

        /// <summary>
        /// Target - TargetIdle (without overhead)
        /// </summary>
        Result,

        /// <summary>
        /// Unknown 
        /// </summary>
        Unknown,
        
        /// <summary>
        /// executing benchmark for the purpose of JIT wamup
        /// </summary>
        MainJitting, IdleJitting
    }
}