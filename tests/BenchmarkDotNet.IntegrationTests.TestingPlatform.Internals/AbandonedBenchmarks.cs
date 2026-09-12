using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// Benchmarks whose parameter values are disposable, used to check what becomes of the values of a request that
    /// is still in flight when the application ends.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class AbandonedBenchmarks
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly Abandoned[] Instances = [new Abandoned(1), new Abandoned(2)];

        public IEnumerable<Abandoned> Values => Instances;

        [ParamsSource(nameof(Values))]
        public Abandoned? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class Abandoned : IDisposable
        {
            public Abandoned(int number) => Number = number;

            public static int Created => Instances.Length;

            public static int Disposed { get; private set; }

            public int Number { get; }

            public void Dispose() => Disposed++;

            public override string ToString() => $"abandoned-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
