using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// The benchmarks this application's requests enumerate. They are never executed - the run request under test is
    /// refused before anything is handed to BenchmarkDotNet - so what they do does not matter, only that there are
    /// several of them to be reported about.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class FilteredBenchmarks
    {
        [Params(1, 2)]
        public int Size { get; set; }

        [Benchmark]
        public int Add() => Size + Size;

        [Benchmark]
        public int Multiply() => Size * Size;

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
