using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, ImmutableConfig config, bool containsBenchmarkDeclarations) : IDisposable
    {
        public BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, ImmutableConfig config) : this(benchmarksCase, type, config, benchmarksCase.Length > 0) { }

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
    }
}