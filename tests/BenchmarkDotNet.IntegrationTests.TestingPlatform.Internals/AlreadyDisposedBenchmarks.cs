using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// Benchmarks whose parameter values are disposable, used to check that a value BenchmarkDotNet already disposed
    /// is not disposed again because a later request enumerated it into a scope of its own.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class AlreadyDisposedBenchmarks
    {
        // Created once, so that re-reading the source cannot change the count.
        private static readonly AlreadyDisposed[] Instances = [new AlreadyDisposed(1), new AlreadyDisposed(2)];

        public IEnumerable<AlreadyDisposed> Values => Instances;

        [ParamsSource(nameof(Values))]
        public AlreadyDisposed? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class AlreadyDisposed : IDisposable
        {
            public AlreadyDisposed(int number) => Number = number;

            public static int Created => Instances.Length;

            public static int Disposed { get; private set; }

            public int Number { get; }

            public void Dispose() => Disposed++;

            public override string ToString() => $"already-disposed-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
