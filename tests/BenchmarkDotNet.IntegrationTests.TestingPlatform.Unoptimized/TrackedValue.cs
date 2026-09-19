namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Unoptimized
{
    /// <summary>
    /// A disposable parameter value that keeps count of how many of its kind were created and disposed. What is left
    /// undisposed only shows at the end, so the counts are written out when the process exits.
    /// </summary>
    public sealed class TrackedValue : IDisposable
    {
        /// <summary>
        /// The name of the file the counts are written to, next to the probe application.
        /// </summary>
        public const string ReportFileName = "unoptimized-probe.txt";

        private static int created;
        private static int disposed;

        static TrackedValue() =>
            AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, ReportFileName),
                $"created={Volatile.Read(ref created)} disposed={Volatile.Read(ref disposed)}");

        public TrackedValue(string name)
        {
            Name = name;
            Interlocked.Increment(ref created);
        }

        public string Name { get; }

        public void Dispose() => Interlocked.Increment(ref disposed);

        public override string ToString() => Name;
    }
}
