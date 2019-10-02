namespace BenchmarkDotNet.Engines
{
    public enum HostSignal
    {
        /// <summary>
        /// before we start the benchmarking process
        /// </summary>
        BeforeProcessStart,

        /// <summary>
        /// before jitting, warmup
        /// </summary>
        BeforeAnythingElse,

        /// <summary>
        /// after globalSetup, warmup and pilot but before the main run
        /// </summary>
        BeforeActualRun,

        /// <summary>
        /// after main run, but before global Cleanup
        /// </summary>
        AfterActualRun,

        /// <summary>
        /// after all (the last thing the benchmarking engine does is to fire this signal)
        /// </summary>
        AfterAll,

        /// <summary>
        /// used to run some code independent to the benchmarked process
        /// </summary>
        SeparateLogic,

        /// <summary>
        /// after the benchmarking process exits
        /// </summary>
        AfterProcessExit
    }
}