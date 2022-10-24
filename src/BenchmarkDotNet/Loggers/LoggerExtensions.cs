using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public static class LoggerExtensions
    {
        public static void WriteLine(this ILogger logger, string text) => logger.WriteLine(LogKind.Default, text);

        public static void WriteLineHelp(this ILogger logger, string text) => logger.WriteLine(LogKind.Help, text);

        public static void WriteLineHeader(this ILogger logger, string text) => logger.WriteLine(LogKind.Header, text);

        public static void WriteLineResult(this ILogger logger, string text) => logger.WriteLine(LogKind.Result, text);

        public static void WriteLineStatistic(this ILogger logger, string text) => logger.WriteLine(LogKind.Statistic, text);

        public static void WriteLineInfo(this ILogger logger, string text) => logger.WriteLine(LogKind.Info, text);

        public static void WriteLineError(this ILogger logger, string text) => logger.WriteLine(LogKind.Error, text);

        public static void WriteLineHint(this ILogger logger, string text) => logger.WriteLine(LogKind.Hint, text);

        public static void Write(this ILogger logger, string text) => logger.Write(LogKind.Default, text);

        [PublicAPI]
        public static void WriteHelp(this ILogger logger, string text) => logger.Write(LogKind.Help, text);

        public static void WriteHeader(this ILogger logger, string text) => logger.Write(LogKind.Header, text);

        [PublicAPI]
        public static void WriteResult(this ILogger logger, string text) => logger.Write(LogKind.Result, text);

        public static void WriteStatistic(this ILogger logger, string text) => logger.Write(LogKind.Statistic, text);

        public static void WriteInfo(this ILogger logger, string text) => logger.Write(LogKind.Info, text);

        public static void WriteError(this ILogger logger, string text) => logger.Write(LogKind.Error, text);

        [PublicAPI]
        public static void WriteHint(this ILogger logger, string text) => logger.Write(LogKind.Hint, text);
    }
}