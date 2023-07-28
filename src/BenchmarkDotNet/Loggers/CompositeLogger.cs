using System.Collections.Immutable;

namespace BenchmarkDotNet.Loggers
{
    internal class CompositeLogger : ILogger
    {
        private readonly ImmutableHashSet<ILogger> loggers;

        internal CompositeLogger(ImmutableHashSet<ILogger> loggers) => this.loggers = loggers;

        public string Id => nameof(CompositeLogger);
        public int Priority => 0;

        public void Write(LogKind logKind, string text)
        {
            // BenchmarkRunner uses a single instance of CompositeLogger,
            // it passes it as ILogger to various components, which may use it in parallel.
            // We need to acquire the lock to avoid race conditions like #2125 and #2264
            lock (this)
            {
                foreach (var logger in loggers)
                {
                    logger.Write(logKind, text);
                }
            }
        }

        public void WriteLine()
        {
            lock (this)
            {
                foreach (var logger in loggers)
                {
                    logger.WriteLine();
                }
            }
        }

        public void WriteLine(LogKind logKind, string text)
        {
            lock (this)
            {
                foreach (var logger in loggers)
                {
                    logger.WriteLine(logKind, text);
                }
            }
        }

        public void Flush()
        {
            lock (this)
            {
                foreach (var logger in loggers)
                {
                    logger.Flush();
                }
            }
        }
    }
}