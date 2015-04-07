using System;

namespace BenchmarkDotNet.Logging
{
    public static class BenchmarkLoggerExtensions
    {
        #region WriteLine

        public static void WriteLine(this IBenchmarkLogger logger, BenchmarkLogKind logKind, string format, params object[] args)
        {
            logger.Write(logKind, (format ?? string.Empty) + Environment.NewLine, args);
        }

        public static void WriteLine(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Default, format, args);
        }

        public static void WriteLineHelp(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Help, format, args);
        }

        public static void WriteLineHeader(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Header, format, args);
        }

        public static void WriteLineResult(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Result, format, args);
        }

        public static void WriteLineStatistic(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Statistic, format, args);
        }

        public static void WriteLineExtraInfo(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.ExtraInfo, format, args);
        }

        public static void WriteLineError(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.WriteLine(BenchmarkLogKind.Error, format, args);
        }

        #endregion

        #region Write

        public static void Write(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Default, format, args);
        }

        public static void WriteHelp(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Help, format, args);
        }

        public static void WriteHeader(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Header, format, args);
        }

        public static void WriteResult(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Result, format, args);
        }

        public static void WriteStatistic(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Statistic, format, args);
        }

        public static void WriteExtraInfo(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.ExtraInfo, format, args);
        }

        public static void WriteError(this IBenchmarkLogger logger, string format, params object[] args)
        {
            logger.Write(BenchmarkLogKind.Error, format, args);
        }

        #endregion

        #region Misc

        public static void NewLine(this IBenchmarkLogger logger)
        {
            logger.Write(BenchmarkLogKind.Default, Environment.NewLine);
        }

        #endregion
    }
}