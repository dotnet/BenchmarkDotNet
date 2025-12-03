using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
public enum BenchmarkSignal
{
    /// <summary>
    /// before the engine is created
    /// </summary>
    BeforeEngine,

    /// <inheritdoc cref="HostSignal.BeforeActualRun"/>
    BeforeActualRun,

    /// <inheritdoc cref="HostSignal.AfterActualRun"/>
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