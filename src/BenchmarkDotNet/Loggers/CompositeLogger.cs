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