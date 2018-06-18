using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Running
{
    public class BenchmarkBuildInfo
    {
        public BenchmarkBuildInfo(BenchmarkCase benchmarkCase, ReadOnlyConfig config, int id)
        {
            BenchmarkCase = benchmarkCase;
            Config = config;
            Id = new BenchmarkId(id);
        }

        public BenchmarkCase BenchmarkCase { get; }

        public ReadOnlyConfig Config { get; }

        public BenchmarkId Id { get; }
    }
}