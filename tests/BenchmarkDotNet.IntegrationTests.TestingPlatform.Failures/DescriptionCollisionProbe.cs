using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Failures
{
    /// <summary>
    /// Two benchmarks without any parameter that BenchmarkDotNet still identifies as one, because the description of
    /// the first is the method name of the second and the identity of a benchmark is built from the name it displays.
    /// The reported collision has to point at that rather than at the parameters.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class DescriptionCollisionProbe
    {
        [Benchmark(Description = "Twin")]
        public int Described() => 1;

        [Benchmark]
        public int Twin() => 2;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
