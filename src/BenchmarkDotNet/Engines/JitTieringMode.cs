namespace BenchmarkDotNet.Engines;

/// <summary>
/// Controls the behavior of the JIT stage when tiering is enabled.
/// </summary>
public enum JitTieringMode
{
    /// <summary>
    /// Default.
    /// If the benchmark is long-running, tiering is skipped and the benchmark method is left at the tier following the initial invoke (usually tier0).
    /// Otherwise, the JIT stage attempts to force the benchmark method and its callees to be promoted to their final tier by calling it repeatedly before measurements begin.
    /// </summary>
    Auto,

    /// <summary>
    /// Forces the JIT stage to attempt to force the benchmark method and its callees to be promoted to their final tier by calling it repeatedly before measurements begin.
    /// Useful when you want the most stable tier1 measurements even for longer-running benchmarks.
    /// </summary>
    Force,

    /// <summary>
    /// Forces the JIT stage to skip tier promotion entirely, leaving the benchmark method at the tier following the initial invoke (usually tier0).
    /// </summary>
    Skip
}
