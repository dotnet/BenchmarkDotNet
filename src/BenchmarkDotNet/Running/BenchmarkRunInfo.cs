using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, ImmutableConfig config, bool containsBenchmarkDeclarations, CompositeInProcessDiagnoser compositeInProcessDiagnoser) : IDisposable
    {
        public BenchmarkRunInfo(BenchmarkCase[] benchmarksCases, Type type, ImmutableConfig config, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
            : this(benchmarksCases, type, config, benchmarksCases.Length > 0, compositeInProcessDiagnoser) { }

        public void Dispose()
        {
            foreach (var benchmarkCase in BenchmarksCases)
            {
                benchmarkCase.Dispose();
            }
        }

        public BenchmarkCase[] BenchmarksCases { get; } = benchmarksCase;
        public Type Type { get; } = type;
        public ImmutableConfig Config { get; } = config;
        public bool ContainsBenchmarkDeclarations { get; } = containsBenchmarkDeclarations;
        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; } = compositeInProcessDiagnoser;
    }
}