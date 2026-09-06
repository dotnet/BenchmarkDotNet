using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose parameter values are disposable. Enumerating the assembly creates every one of them, whether
    /// the case it belongs to is going to run or not, and BenchmarkDotNet only disposes the ones it was handed - a
    /// parameter with a locking finalizer hangs the runtime otherwise, see dotnet/BenchmarkDotNet#1383. What is left
    /// undisposed only shows at the end, so the counts are written out when the process exits.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class DisposableProbe
    {
        /// <summary>
        /// The name of the file the counts are written to, next to the probe application.
        /// </summary>
        public const string ReportFileName = "disposable-probe.txt";

        // Created once, so that re-reading the source cannot change the count.
        private static readonly Tracked[] Instances = [new Tracked(1), new Tracked(2), new Tracked(3)];

        public IEnumerable<Tracked> Values => Instances;

        [ParamsSource(nameof(Values))]
        public Tracked? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        public class Tracked : IDisposable
        {
            private static int disposed;

            static Tracked() =>
                AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllText(
                    Path.Combine(AppContext.BaseDirectory, ReportFileName),
                    $"created={Instances.Length} disposed={Volatile.Read(ref disposed)}");

            public Tracked(int number) => Number = number;

            public int Number { get; }

            public void Dispose() => Interlocked.Increment(ref disposed);

            public override string ToString() => $"tracked-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
