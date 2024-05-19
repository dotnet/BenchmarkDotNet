using BenchmarkDotNet.Loggers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Text;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class to send logs from BDN to the VSTest output log.
    /// </summary>
    internal sealed class VsTestLogger : ILogger
    {
        private readonly IMessageLogger messageLogger;
        private readonly StringBuilder currentLine = new StringBuilder();
        private TestMessageLevel currentLevel = TestMessageLevel.Informational;

        public VsTestLogger(IMessageLogger logger)
        {
            messageLogger = logger;
        }

        public string Id => nameof(VsTestLogger);

        public int Priority => 0;

        public void Flush()
        {
            WriteLine();
        }

        public void Write(LogKind logKind, string text)
        {
            currentLine.Append(text);

            // Assume that if the log kind is an error, that the whole line is treated as an error
            // The level will be reset to Informational when WriteLine() is called.
            currentLevel = logKind switch
            {
                LogKind.Error => TestMessageLevel.Error,
                LogKind.Warning => TestMessageLevel.Warning,
                _ => currentLevel
            };
        }

        public void WriteLine()
        {
            // The VSTest logger throws an error on logging empty or whitespace strings, so skip them.
            if (currentLine.Length == 0)
                return;

            messageLogger.SendMessage(currentLevel, currentLine.ToString());

            currentLevel = TestMessageLevel.Informational;
            currentLine.Clear();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Write(logKind, text);
            WriteLine();
        }
    }
}
