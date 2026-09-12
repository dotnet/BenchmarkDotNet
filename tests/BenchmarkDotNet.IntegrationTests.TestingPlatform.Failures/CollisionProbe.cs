using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Failures
{
    /// <summary>
    /// Two benchmark cases that BenchmarkDotNet identifies as one, because a benchmark is identified by the string
    /// representation of its parameters and both values stringify the same way. The platform cannot tell the two apart
    /// either, so the adapter is expected to report the collision instead of running them.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class CollisionProbe
    {
        public IEnumerable<Ambiguous> Values => [new Ambiguous(1), new Ambiguous(2)];

        [ParamsSource(nameof(Values))]
        public Ambiguous? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class Ambiguous(int number)
        {
            public int Number { get; } = number;

            public override string ToString() => "ambiguous";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
