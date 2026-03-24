namespace BenchmarkDotNet.Code;

/// <summary>
/// Specifies how to generate the entry-point for the benchmark process.
/// </summary>
public enum CodeGenEntryPointType
{
    Synchronous,
    Asynchronous
}
