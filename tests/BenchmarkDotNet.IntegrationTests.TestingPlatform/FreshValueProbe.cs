using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform
{
    /// <summary>
    /// A benchmark whose parameter source constructs its values on every read - the common shape of a source over
    /// resources, <c>yield return new FileStream(...)</c> - as opposed to <see cref="DisposableProbe"/>, which hands
    /// back the same instances every time. Under server mode every request reads the source again, and nothing a
    /// later request could reuse comes of it, so the values of one request must not be held for the whole session.
    /// </summary>
    /// <remarks>
    /// The report is a line per read, written as the read happens, so that a test driving several requests from one
    /// process can see what had been disposed by the time each request enumerated - and a last line at exit.
    /// </remarks>
    [Config(typeof(FastConfig))]
    public class FreshValueProbe
    {
        /// <summary>
        /// The name of the file the counts are written to, next to the probe application.
        /// </summary>
        public const string ReportFileName = "fresh-value-probe.txt";

        private static int reads;
        private static int created;
        private static int disposed;

        static FreshValueProbe() =>
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Report($"exit created={Volatile.Read(ref created)} disposed={Volatile.Read(ref disposed)}");

        public static IEnumerable<Fresh> Values
        {
            get
            {
                var read = Interlocked.Increment(ref reads);
                Fresh[] values = [new Fresh(1), new Fresh(2)];

                Report($"read={read} created={Volatile.Read(ref created)} disposed={Volatile.Read(ref disposed)}");

                return values;
            }
        }

        [ParamsSource(nameof(Values))]
        public Fresh? Value { get; set; }

        [Benchmark]
        public int Identity() => Value!.Number;

        private static void Report(string line)
            => File.AppendAllText(Path.Combine(AppContext.BaseDirectory, ReportFileName), line + Environment.NewLine);

        public class Fresh : IDisposable
        {
            public Fresh(int number)
            {
                Number = number;
                Interlocked.Increment(ref created);
            }

            public int Number { get; }

            public void Dispose() => Interlocked.Increment(ref disposed);

            // The same name on every read, so that a benchmark keeps its identity across the requests of a session.
            public override string ToString() => $"fresh-{Number}";
        }

        private class FastConfig : ManualConfig
        {
            public FastConfig() => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default));
        }
    }
}
