using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

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

        public void Write(LogKind logKind, string format, params object[] args)
        {
            var s = string.Format(EnvironmentHelper.MainCultureInfo, format, args).AddPrefixMultiline(Prefix);
            if (!isNewLine)
                s = s.Remove(0, Prefix.Length);
            Logger.Write(logKind, s);
            isNewLine = s.EndsWith("\n");
        }
    }
}