using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Plugins.Loggers
{
    public class BenchmarkLoggerWithPrefix : IBenchmarkLogger
    {
        public string Name => "prefix logger";
        public string Description => "adds prefix for each line";

        public IBenchmarkLogger Logger { get; }
        public string Prefix { get; }
        private bool isNewLine = true;

        public BenchmarkLoggerWithPrefix(IBenchmarkLogger logger, string prefix)
        {
            Logger = logger;
            Prefix = prefix;
        }

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            var s = string.Format(EnvironmentInfo.MainCultureInfo, format, args).AddPrefixMultiline(Prefix);
            if (!isNewLine)
                s = s.Remove(0, Prefix.Length);
            Logger.Write(logKind, s);
            isNewLine = s.EndsWith("\n");
        }
    }
}