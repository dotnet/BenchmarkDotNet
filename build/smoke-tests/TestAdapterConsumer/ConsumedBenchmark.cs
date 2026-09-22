using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace TestAdapterConsumer;

/// <summary>
/// A single benchmark, enough for the smoke test to check that the packaged adapter turns this project into a
/// Microsoft.Testing.Platform application that can list it.
/// </summary>
[Config(typeof(FastConfig))]
public class ConsumedBenchmark
{
    [Benchmark]
    public int Add() => 1 + 1;

    private class FastConfig : ManualConfig
    {
        public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
    }
}
