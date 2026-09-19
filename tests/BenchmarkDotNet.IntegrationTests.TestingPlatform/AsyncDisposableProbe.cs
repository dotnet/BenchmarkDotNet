using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose parameter values come from an async source and are only <see cref="IAsyncDisposable"/>. They
    /// are disposed by exactly the same rules as a synchronously disposable one, and casting a value to
    /// <see cref="IDisposable"/> would drop them on the floor instead - which the finalizer of a value holding a lock
    /// then turns into the hang of dotnet/BenchmarkDotNet#1383.
    /// </summary>
    [Config(typeof(FastConfig))]
    public class AsyncDisposableProbe
    {
        /// <summary>
        /// The name of the file the counts are written to, next to the probe application.
        /// </summary>
        public const string ReportFileName = "async-disposable-probe.txt";

        // Created once, so that re-reading the source cannot change the count.
        private static readonly AsyncTracked[] Instances = [new AsyncTracked(1), new AsyncTracked(2)];

        public static async IAsyncEnumerable<object> GetValues()
        {
            await Task.Yield();

            foreach (var instance in Instances)
                yield return instance;
        }

        [ParamsSource(nameof(GetValues))]
        public AsyncTracked? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        /// <summary>
        /// Deliberately not <see cref="IDisposable"/>: the whole point of the probe.
        /// </summary>
        public class AsyncTracked : IAsyncDisposable
        {
            private static int disposed;

            static AsyncTracked() =>
                AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllText(
                    Path.Combine(AppContext.BaseDirectory, ReportFileName),
                    $"created={Instances.Length} disposed={Volatile.Read(ref disposed)}");

            public AsyncTracked(int number) => Number = number;

            public int Number { get; }

            public ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref disposed);
                return default;
            }

            public override string ToString() => $"async-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
