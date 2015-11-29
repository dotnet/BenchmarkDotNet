namespace BenchmarkDotNet.Plugins.Loggers
{
    public class BenchmarkCompositeLogger : IBenchmarkLogger
    {
        public string Name => "composite";
        public string Description => "Composite logger";

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