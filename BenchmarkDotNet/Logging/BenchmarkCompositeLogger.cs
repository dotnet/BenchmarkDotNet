namespace BenchmarkDotNet.Logging
{
    public class BenchmarkCompositeLogger : IBenchmarkLogger
    {
        private readonly IBenchmarkLogger[] loggers;

        public BenchmarkCompositeLogger(params IBenchmarkLogger[] loggers)
        {
            this.loggers = loggers;
        }

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            foreach (var logger in loggers)
                logger.Write(logKind, format, args);
        }
    }
}