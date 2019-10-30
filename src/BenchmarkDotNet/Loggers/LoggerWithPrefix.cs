using System;

namespace BenchmarkDotNet.Loggers
{
    /// <summary>
    /// Adds prefix for each line
    /// </summary>
    public class LoggerWithPrefix : ILogger
    {
        private ILogger Logger { get; }
        private string Prefix { get; }
        private bool isNewLine = true;

        public LoggerWithPrefix(ILogger logger, string prefix)
        {
            Logger = logger;
            Prefix = prefix;
            Id = nameof(LoggerWithPrefix) + "." + Logger.Id + "." + Prefix;
        }

        public string Id { get; }
        public int Priority => Logger.Priority;

        public void Write(LogKind logKind, string text)
        {
            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            WriteSimple(logKind, lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                WriteLine();
                WriteSimple(logKind, lines[i]);
            }
        }

        private void WriteSimple(LogKind logKind, string text)
        {
            AddPrefixIfNeeded(logKind, text);
            Logger.Write(text);
        }

        private void AddPrefixIfNeeded(LogKind logKind, string text)
        {
            if (isNewLine && !string.IsNullOrEmpty(text))
            {
                Logger.Write(logKind, Prefix);
                isNewLine = false;
            }
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Write(logKind, text);
            WriteLine();
        }

        public void WriteLine()
        {
            Logger.WriteLine();

            isNewLine = true;
        }

        public void Flush() { }
    }
}