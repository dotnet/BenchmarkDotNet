namespace BenchmarkDotNet.Loggers
{
    public class NullLogger : ILogger
    {
        public static readonly ILogger Instance = new NullLogger();

        private NullLogger() { }

        public void Write(LogKind logKind, string text) { }

        public void WriteLine() { }

        public void WriteLine(LogKind logKind, string text) { }
    }
}