using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkBuildInfo
    {
        public BenchmarkBuildInfo(Benchmark benchmark, ReadOnlyConfig config, int id)
        {
            Benchmark = benchmark;
            Config = config;
            Id = new BenchmarkId(id);
        }

        public Benchmark Benchmark { get; }

        public ReadOnlyConfig Config { get; }

        public BenchmarkId Id { get; }
    }
}