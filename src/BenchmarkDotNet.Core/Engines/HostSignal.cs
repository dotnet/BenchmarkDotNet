namespace BenchmarkDotNet.Engines
{
    public enum HostSignal
    {
        /// <summary>
        /// before jitting, warmup
        /// </summary>
        BeforeAnythingElse,

        /// <summary>
        /// after globalSetup, warmup and pilot but before the main run
        /// </summary>
        BeforeMainRun,

        /// <summary>
        /// after main run, but before global Cleanup
        /// </summary>
        AfterMainRun,

        /// <summary>
        /// after all (the last thing the benchmarking engine does is to fire this signal)
        /// </summary>
        AfterAll
    }
}