using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
public enum BenchmarkSignal
{
    /// <summary>
    /// before the engine is created
    /// </summary>
    BeforeEngine,

    /// <summary>
    /// after globalSetup, warmup and pilot but before the main run
    /// </summary>
    BeforeActualRun,

    /// <summary>
    /// after main run, but before global Cleanup
    /// </summary>
    AfterActualRun,

    /// <summary>
    /// after the engine has completed the run
    /// </summary>
    AfterEngine,

    /// <summary>
    /// used to run some code independent of the benchmarks
    /// </summary>
    SeparateLogic,
}