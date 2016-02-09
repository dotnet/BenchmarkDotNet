using System;

namespace BenchmarkDotNet.Loggers
{
    public static class LoggerExtensions
    {
        #region WriteLine

        public static void WriteLine(this ILogger logger, LogKind logKind, string format, params object[] args) => 
            logger.Write(logKind, (format ?? string.Empty) + Environment.NewLine, args);

        public static void WriteLine(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Default, format, args);

        public static void WriteLine(this ILogger logger) => 
            logger.WriteLine(LogKind.Default, "");

        public static void WriteLineHelp(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Help, format, args);

        public static void WriteLineHeader(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Header, format, args);

        public static void WriteLineResult(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Result, format, args);

        public static void WriteLineStatistic(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Statistic, format, args);

        public static void WriteLineInfo(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Info, format, args);

        public static void WriteLineError(this ILogger logger, string format, params object[] args) => 
            logger.WriteLine(LogKind.Error, format, args);

        #endregion

        #region Write

        public static void Write(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Default, format, args);

        public static void WriteHelp(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Help, format, args);

        public static void WriteHeader(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Header, format, args);

        public static void WriteResult(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Result, format, args);

        public static void WriteStatistic(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Statistic, format, args);

        public static void WriteInfo(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Info, format, args);

        public static void WriteError(this ILogger logger, string format, params object[] args) => 
            logger.Write(LogKind.Error, format, args);

        #endregion

        #region Misc

        public static void NewLine(this ILogger logger) => 
            logger.Write(LogKind.Default, Environment.NewLine);

        #endregion
    }
}