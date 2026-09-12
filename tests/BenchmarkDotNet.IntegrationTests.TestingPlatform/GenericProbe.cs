using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A generic benchmark, used to check how closed generic types are named and grouped by test runners.
    /// </summary>
    [Config(typeof(GenericProbeConfig))]
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(char))]
    [GenericTypeArguments(typeof(System.Collections.Generic.List<string>))]
    public class GenericProbe<T> where T : new()
    {
        [Benchmark]
        public T Create() => new T();
    }

    internal class GenericProbeConfig : ManualConfig
    {
        public GenericProbeConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
    }
}
