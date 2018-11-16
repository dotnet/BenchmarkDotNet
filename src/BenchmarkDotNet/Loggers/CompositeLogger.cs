namespace BenchmarkDotNet.Loggers
{
    public class CompositeLogger : ILogger
    {
        private readonly ILogger[] loggers;

        public CompositeLogger(params ILogger[] loggers)
        {
            this.loggers = loggers;
        }

        public void Write(LogKind logKind, string text)
        {
            foreach (var logger in loggers)
                logger.Write(logKind, text);
        }

        public void WriteLine()
        {
            foreach (var logger in loggers)
                logger.WriteLine();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            foreach (var logger in loggers)
                logger.WriteLine(logKind, text);
        }

        public void Flush()
        {
            foreach (var logger in loggers)
                logger.Flush();
        }
    }
}