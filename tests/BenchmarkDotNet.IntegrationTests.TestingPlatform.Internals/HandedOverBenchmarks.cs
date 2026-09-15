using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// Benchmarks whose parameter values are disposable, used to check what becomes of the values of a request that
    /// had already handed them to BenchmarkDotNet when the application ended.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class HandedOverBenchmarks
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly HandedOver[] Instances = [new HandedOver(1), new HandedOver(2)];

        public IEnumerable<HandedOver> Values => Instances;

        [ParamsSource(nameof(Values))]
        public HandedOver? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class HandedOver : IDisposable
        {
            public HandedOver(int number) => Number = number;

            public static int Created => Instances.Length;

            public static int Disposed { get; private set; }

            public int Number { get; }

            public void Dispose() => Disposed++;

            public override string ToString() => $"handed-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
