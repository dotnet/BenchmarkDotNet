
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Extensions;

public static class BenchmarkCaseExtensions
{
    /// <summary>
    /// Gets the unique ID for the given BenchmarkCase.
    /// </summary>
    public static string GetUniqueId(this BenchmarkCase benchmarkCase)
        => UniqueIdGenerator.FromBenchmarkCase(benchmarkCase);
}
