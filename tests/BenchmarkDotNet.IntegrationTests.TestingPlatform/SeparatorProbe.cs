using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose parameter contains the character Microsoft.Testing.Platform uses to separate the levels of the
    /// tree a <c>--treenode-filter</c> walks. It has to stay at the same level of that tree as every other benchmark.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class SeparatorProbe
    {
        [Params("a/b")]
        public string Value { get; set; } = "";

        [Benchmark]
        public int Length() => Value.Length;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
