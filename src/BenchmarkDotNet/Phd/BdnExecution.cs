using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using Perfolizer.Horology;
using Perfolizer.Phd.Dto;

namespace BenchmarkDotNet.Phd;

public class BdnExecution : PhdExecution
{
    /// <summary>
    /// Available values: Throughput and ColdStart.
    ///     Throughput: default strategy which allows to get good precision level.
    ///     ColdStart: should be used only for measuring cold start of the application or testing purpose.
    ///     Monitoring: no overhead evaluating, with several target iterations. Perfect for macrobenchmarks without a steady state with high variance.
    /// </summary>
    public RunStrategy? RunStrategy { get; set; }

    /// <summary>
    /// How many times we should launch process with target benchmark.
    /// </summary>
    public int? LaunchCount { get; set; }

    /// <summary>
    /// How many warmup iterations should be performed.
    /// </summary>
    public int? WarmupCount { get; set; }

    /// <summary>
    /// How many target iterations should be performed
    /// If specified, <see cref="MinIterationCount"/> will be ignored.
    /// If specified, <see cref="MaxIterationCount"/> will be ignored.
    /// </summary>
    public int? IterationCount { get; set; }

    /// <summary>
    /// Desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration.
    /// The default value is 500 milliseconds.
    /// </summary>
    public long? IterationTimeMs { get; set; }

    /// <summary>
    /// Invocation count in a single iteration.
    /// If specified, <see cref="IterationTimeMs"/> will be ignored.
    /// If specified, it must be a multiple of <see cref="UnrollFactor"/>.
    /// </summary>
    public long? InvocationCount { get; set; }

    /// <summary>
    /// How many times the benchmark method will be invoked per one iteration of a generated loop.
    /// </summary>
    public int? UnrollFactor { get; set; }

    /// <summary>
    /// Minimum count of target iterations that should be performed
    /// The default value is 15
    /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/> is not going to work</remarks>
    /// </summary>
    public int? MinIterationCount { get; set; }

    /// <summary>
    /// Maximum count of target iterations that should be performed
    /// The default value is 100
    /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/>  is not going to work</remarks>
    /// </summary>
    public int? MaxIterationCount { get; set; }

    /// <summary>
    /// Minimum count of warmup iterations that should be performed
    /// The default value is 6
    /// </summary>
    public int? MinWarmupIterationCount { get; set; }

    /// <summary>
    /// Maximum count of warmup iterations that should be performed
    /// The default value is 50
    /// </summary>
    public int? MaxWarmupIterationCount { get; set; }

    /// <summary>
    /// specifies whether Engine should allocate some random-sized memory between iterations
    /// <remarks>it makes [GlobalCleanup] and [GlobalSetup] methods to be executed after every iteration</remarks>
    /// </summary>
    public bool? MemoryRandomization { get; set; }
}