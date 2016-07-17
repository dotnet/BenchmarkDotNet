using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Loggers
{
    /// <summary>
    /// Adds prefix for each line
    /// </summary>
    public class LoggerWithPrefix : ILogger
    {
        public ILogger Logger { get; }
        public string Prefix { get; }
        private bool isNewLine = true;

        public LoggerWithPrefix(ILogger logger, string prefix)
        {
            Logger = logger;
            Prefix = prefix;
        }

        public void Write(LogKind logKind, string text)
        {
            if (isNewLine)
            {
                Logger.Write(logKind, Prefix);
                isNewLine = false;
            }

            Logger.Write(logKind, text);
        }

        public void WriteLine()
        {
            Logger.WriteLine();

            isNewLine = true;
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Logger.Write(logKind, Prefix);
            Logger.WriteLine(logKind, text);

            isNewLine = true;
        }
    }
}