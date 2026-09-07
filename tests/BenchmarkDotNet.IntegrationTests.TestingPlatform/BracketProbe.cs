using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose parameter contains the characters a <c>--treenode-filter</c> uses to delimit a property
    /// filter. The leaf of every path ends in the job between those same characters, so a filter has to be able to
    /// spell them out rather than have them parsed.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class BracketProbe
    {
        [Params("[Dry]")]
        public string Value { get; set; } = "";

        [Benchmark]
        public int Length() => Value.Length;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
