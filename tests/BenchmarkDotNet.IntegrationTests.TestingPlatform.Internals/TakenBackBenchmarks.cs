using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// Benchmarks whose parameter values are disposable, used to check what becomes of the values of a request that
    /// handed them to BenchmarkDotNet and took them back again, as one whose run was stopped by a critical validation
    /// error does.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class TakenBackBenchmarks
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly TakenBack[] Instances = [new TakenBack(1), new TakenBack(2)];

        public IEnumerable<TakenBack> Values => Instances;

        [ParamsSource(nameof(Values))]
        public TakenBack? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class TakenBack : IDisposable
        {
            public TakenBack(int number) => Number = number;

            public static int Created => Instances.Length;

            public static int Disposed { get; private set; }

            public int Number { get; }

            public void Dispose() => Disposed++;

            public override string ToString() => $"taken-back-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
