using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkBuildInfo
    {
        public BenchmarkBuildInfo(BenchmarkCase benchmarkCase, ImmutableConfig config, int id, CompositeInProcessDiagnoser compositeInProcessDiagnoser)
        {
            BenchmarkCase = benchmarkCase;
            Config = config;
            Id = new BenchmarkId(id, benchmarkCase);
            CompositeInProcessDiagnoser = compositeInProcessDiagnoser;
        }

        public BenchmarkCase BenchmarkCase { get; }

        public ImmutableConfig Config { get; }

        public BenchmarkId Id { get; }

        public CompositeInProcessDiagnoser CompositeInProcessDiagnoser { get; }
    }
}