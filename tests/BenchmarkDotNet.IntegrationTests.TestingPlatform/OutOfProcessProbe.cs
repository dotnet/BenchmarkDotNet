using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark that runs out of process, on the default toolchain. The other probes stay in process to keep the run
    /// fast, which skips the generate/build/execute cycle entirely, so this one is what makes the adapter see a real
    /// <see cref="BenchmarkDotNet.EventProcessors.EventProcessor.OnBuildComplete"/>.
    /// </summary>
    [Config(typeof(OutOfProcessConfig))]
    public class OutOfProcessProbe
    {
        [Benchmark]
        public int Add() => 1 + 1;

        private class OutOfProcessConfig : ManualConfig
        {
            // A dry job on the default toolchain: one iteration, but a separate executable is still generated, built
            // and run.
            public OutOfProcessConfig() => AddJob(Job.Dry);
        }
    }
}
