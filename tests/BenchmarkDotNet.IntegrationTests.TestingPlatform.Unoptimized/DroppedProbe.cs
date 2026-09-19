using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Unoptimized
{
    /// <summary>
    /// A benchmark that only runs out of process, so an unoptimized assembly hides every one of its cases and nothing
    /// downstream can reach the parameter values they own. This is the leak: they are disposed by nobody unless the
    /// enumeration disposes them itself.
    /// </summary>
    [Config(typeof(OutOfProcessConfig))]
    public class DroppedProbe
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly TrackedValue[] Instances = [new TrackedValue("dropped-1"), new TrackedValue("dropped-2")];

        public IEnumerable<TrackedValue> Values => Instances;

        [ParamsSource(nameof(Values))]
        public TrackedValue? Value { get; set; }

        [Benchmark]
        public int Length() => Value!.Name.Length;

        private class OutOfProcessConfig : ManualConfig
        {
            // A dry job on the default toolchain, which generates, builds and runs a separate executable - except
            // that it is never reached here, because the enumeration hides it before anything is built.
            public OutOfProcessConfig() => AddJob(Job.Dry.WithId("OutOfProcess"));
        }
    }
}
