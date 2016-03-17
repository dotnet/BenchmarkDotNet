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
            // this logic seems crazy to me
            var s = text.AddPrefixMultiline(Prefix);
            if (!isNewLine)
                s = s.Remove(0, Prefix.Length);
            Logger.Write(logKind, s);
            isNewLine = s.EndsWith("\n");
        }

        public void WriteLine()
        {
            Logger.WriteLine();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Logger.Write(logKind, Prefix);
            Logger.WriteLine(logKind, text);
        }
    }
}