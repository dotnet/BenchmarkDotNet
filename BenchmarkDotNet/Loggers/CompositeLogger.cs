namespace BenchmarkDotNet.Loggers
{
    public class CompositeLogger : ILogger
    {
        private readonly ILogger[] loggers;

        public CompositeLogger(params ILogger[] loggers)
        {
            this.loggers = loggers;
        }

        public void Write(LogKind logKind, string format, params object[] args)
        {
            foreach (var logger in loggers)
                logger.Write(logKind, format, args);
        }
    }
}