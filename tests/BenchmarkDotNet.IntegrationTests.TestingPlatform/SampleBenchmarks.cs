using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// Benchmarks used to exercise the Microsoft.Testing.Platform adapter end to end. They run in-process with a
    /// single iteration so that a full run stays fast.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class SampleBenchmarks
    {
        [Params(1, 2)]
        public int Size { get; set; }

        [Benchmark]
        [BenchmarkCategory("Fast")]
        public int Add() => Size + Size;

        [Benchmark]
        [BenchmarkCategory("Slow")]
        public int Multiply() => Size * Size;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
