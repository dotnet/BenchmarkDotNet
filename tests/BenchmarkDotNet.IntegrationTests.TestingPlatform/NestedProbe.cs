using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark declared inside another type. ECMA-335 qualifies a nested type by its declaring types rather than
    /// by its namespace, which the identity a test runner reads has to follow.
    /// </summary>
    public static class NestedProbe
    {
        [Config(typeof(FastConfig))]
        public class Inner
        {
            [Benchmark]
            public int Identity() => 1;
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
