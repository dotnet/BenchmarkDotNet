using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Unoptimized
{
    /// <summary>
    /// A benchmark that runs under an in-process and an out-of-process job over the same parameter values. An
    /// unoptimized assembly hides the out-of-process cases only, and BenchmarkConverter hands the very same
    /// ParameterInstance to both jobs, so disposing what is hidden case by case would take down values the surviving
    /// benchmarks still own.
    /// </summary>
    [Config(typeof(BothToolchainsConfig))]
    public class SharedValueProbe
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly TrackedValue[] Instances = [new TrackedValue("shared-1"), new TrackedValue("shared-2")];

        public IEnumerable<TrackedValue> Values => Instances;

        [ParamsSource(nameof(Values))]
        public TrackedValue? Value { get; set; }

        [Benchmark]
        public int Length() => Value!.Name.Length;

        private class BothToolchainsConfig : ManualConfig
        {
            // The ids are set explicitly, so that the two jobs stay distinguishable by name.
            public BothToolchainsConfig()
            {
                AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default).WithId("InProcess"));
                AddJob(Job.Dry.WithId("OutOfProcess"));
            }
        }
    }
}
