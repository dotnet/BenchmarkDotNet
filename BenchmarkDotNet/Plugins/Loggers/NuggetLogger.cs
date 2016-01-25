#if DNX451
using ILogger = NuGet.Logging.ILogger;

namespace BenchmarkDotNet.Plugins.Loggers
{
    internal class NugetLogger : ILogger
    {
        public NugetLogger(IBenchmarkLogger benchmarkLogger)
        {
            this.BenchmarkLogger = benchmarkLogger;
        }

        private IBenchmarkLogger BenchmarkLogger { get; }

        public void LogDebug(string data)
        {
            BenchmarkLogger.WriteLine(BenchmarkLogKind.Default, data);
        }

        public void LogVerbose(string data)
        {
            BenchmarkLogger.WriteLine(BenchmarkLogKind.Default, data);
        }

        public void LogInformation(string data)
        {
            BenchmarkLogger.WriteLineInfo(data);
        }

        public void LogWarning(string data)
        {
            BenchmarkLogger.WriteLine(BenchmarkLogKind.Default, data); // todo: maybe we should add warnings?
        }

        public void LogError(string data)
        {
            BenchmarkLogger.WriteLineError(data);
        }
    }
}
#endif