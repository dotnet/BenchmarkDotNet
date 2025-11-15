using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkRunInfo : IDisposable
    {
        public BenchmarkRunInfo(BenchmarkCase[] benchmarksCase, Type type, ImmutableConfig config, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
        {
            BenchmarksCases = benchmarksCase;
            Type = type;
            Config = config;
            CompositeInProcessDiagnoser = compositeInProcessDiagnoser;
        }

        public void Dispose()
        {
            foreach (var benchmarkCase in BenchmarksCases)
            {
                benchmarkCase.Dispose();
            }
        }

        public BenchmarkCase[] BenchmarksCases { get; }
        public Type Type { get; }
        public ImmutableConfig Config { get; }
        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; }
    }
}