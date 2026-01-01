namespace BenchmarkDotNet.Code;

/// <summary>
/// Specifies how to generate the code that calls the benchmark's Run method.
/// </summary>
public enum CodeGenBenchmarkRunCallType
{
    /// <summary>
    /// Use reflection to call the benchmark's Run method indirectly.
    /// </summary>
    /// <remarks>
    /// This is to avoid strong dependency Main-to-Runnable
    /// which could cause the jitting/assembly loading to happen before we do anything.
    /// We have some jitting diagnosers and we want them to catch all the informations.
    /// </remarks>
    Reflection,
    /// <summary>
    /// Uses a switch to select the benchmark to call Run directly.
    /// </summary>
    /// <remarks>
    /// This is for AOT runtimes where reflection may not exist or the benchmark types
    /// could be trimmed out when they are not directly referenced.
    /// </remarks>
    Direct
}
