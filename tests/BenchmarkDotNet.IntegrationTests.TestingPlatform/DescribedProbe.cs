using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// Benchmarks whose display name comes from the description rather than the method name.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class DescribedProbe
    {
        [Params(1)]
        public int Size { get; set; }

        [Benchmark(Description = "A described benchmark")]
        public int Described() => Size;

        [Benchmark]
        public int Undescribed() => Size;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
