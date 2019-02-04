using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkBuildInfo
    {
        public BenchmarkBuildInfo(BenchmarkCase benchmarkCase, ImmutableConfig config, int id)
        {
            BenchmarkCase = benchmarkCase;
            Config = config;
            Id = new BenchmarkId(id, benchmarkCase);
        }

        public BenchmarkCase BenchmarkCase { get; }

        public ImmutableConfig Config { get; }

        public BenchmarkId Id { get; }
    }
}