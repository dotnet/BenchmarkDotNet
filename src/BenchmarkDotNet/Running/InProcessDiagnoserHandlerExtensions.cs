using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Running;

internal static class InProcessDiagnoserHandlerExtensions
{
    // The handler types of the in-process diagnosers configured for a build. They are compiled into the
    // generated benchmark, so a build failure that references one of them is a missing-reference mistake.
    internal static IReadOnlyList<Type> GetInProcessDiagnoserHandlerTypes(this BuildPartition buildPartition)
        => buildPartition.Benchmarks
            .SelectMany(benchmark => benchmark.CompositeInProcessDiagnoser.GetInProcessDiagnoserHandlerTypes(benchmark.BenchmarkCase))
            .Distinct()
            .ToArray();

    internal static IReadOnlyList<Type> GetInProcessDiagnoserHandlerTypes(this CompositeInProcessDiagnoser compositeInProcessDiagnoser, BenchmarkCase benchmarkCase)
        => compositeInProcessDiagnoser.GetHandlerData(benchmarkCase)
            .Select(handlerData => handlerData.HandlerType)
            .WhereNotNull()
            .Distinct()
            .ToArray();
}
