namespace BenchmarkDotNet.Loggers
{
    public class NullLogger : ILogger
    {
        public static readonly ILogger Instance = new NullLogger();

        private NullLogger() { }

        public string Id => nameof(NullLogger);
        public int Priority => 0;
        public void Write(LogKind logKind, string text) { }

        public void WriteLine() { }

        public void WriteLine(LogKind logKind, string text) { }

        public void Flush() { }
    }
}