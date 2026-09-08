using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, ImmutableConfig config, bool containsBenchmarkDeclarations, CompositeInProcessDiagnoser compositeInProcessDiagnoser) : IDisposable, IAsyncDisposable
    {
        public BenchmarkRunInfo(BenchmarkCase[] benchmarksCases, Type type, ImmutableConfig config, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
            : this(benchmarksCases, type, config, benchmarksCases.Length > 0, compositeInProcessDiagnoser) { }

        public ValueTask DisposeAsync() => BenchmarksCases.DisposeAllAsync();

        public void Dispose()
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            context.ExecuteUntilComplete(DisposeAsync());
        }

        public BenchmarkCase[] BenchmarksCases { get; } = benchmarksCase;
        public Type Type { get; } = type;
        public ImmutableConfig Config { get; } = config;
        public bool ContainsBenchmarkDeclarations { get; } = containsBenchmarkDeclarations;
        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; } = compositeInProcessDiagnoser;
    }
}