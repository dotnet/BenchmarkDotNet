namespace BenchmarkDotNet.Engines
{
    public enum IterationStage : int
    {
        Jitting,
        
        /// <summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Pilot_experiment"/>
        /// </summary>
        Pilot,
        
        Warmup,
        
        Actual,
        
        Result,
        
        Unknown
    }
}