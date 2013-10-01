using System;
using System.IO;

namespace BenchmarkDotNet
{
    public static class ConsoleHelper
    {
        static ConsoleHelper()
        {
            defaultOut = Console.Out;
            UseColorfulScheme();
        }

        #region Out

        private static readonly TextWriter defaultOut;

        public static void SetOut(TextWriter newOut)
        {
            Console.SetOut(newOut);
        }

        public static void RestoreDefaultOut()
        {
            Console.SetOut(defaultOut);
        }

        #endregion

        #region ColorScheme

        public static ConsoleColor DefaultColor { get; set; }
        public static ConsoleColor HelpColor { get; set; }
        public static ConsoleColor HeaderColor { get; set; }
        public static ConsoleColor ResultColor { get; set; }
        public static ConsoleColor StatisticColor { get; set; }

        public static void UseColorfulScheme()
        {
            DefaultColor = ConsoleColor.Gray;
            HelpColor = ConsoleColor.DarkGreen;
            HeaderColor = ConsoleColor.Red;
            ResultColor = ConsoleColor.DarkCyan;
            StatisticColor = ConsoleColor.Cyan;
        }

        public static void UseGrayScheme()
        {
            DefaultColor = ConsoleColor.Gray;
            HelpColor = ConsoleColor.Gray;
            HeaderColor = ConsoleColor.Gray;
            ResultColor = ConsoleColor.Gray;
            StatisticColor = ConsoleColor.Gray;
        }

        #endregion

        #region WriteLine

        public static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(BenchmarkUtils.CultureFormat(format, args));
            Console.ForegroundColor = DefaultColor;
        }

        public static void WriteLineDefault(string format, params object[] args)
        {
            WriteLine(DefaultColor, format, args);
        }

        public static void WriteLineHelp(string format, params object[] args)
        {
            WriteLine(HelpColor, format, args);
        }

        public static void WriteLineHeader(string format, params object[] args)
        {
            WriteLine(HeaderColor, format, args);
        }

        public static void WriteLineResult(string format, params object[] args)
        {
            WriteLine(ResultColor, format, args);
        }

        public static void WriteLineStatistic(string format, params object[] args)
        {
            WriteLine(StatisticColor, format, args);
        }

        public static void NewLine()
        {
            Console.WriteLine();
        }

        #endregion

        #region Write

        public static void Write(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.Write(BenchmarkUtils.CultureFormat(format, args));
            Console.ForegroundColor = DefaultColor;
        }

        public static void WriteDefault(string format, params object[] args)
        {
            Write(DefaultColor, format, args);
        }

        public static void WriteHelp(string format, params object[] args)
        {
            Write(HelpColor, format, args);
        }

        public static void WriteHeader(string format, params object[] args)
        {
            Write(HeaderColor, format, args);
        }

        public static void WriteResult(string format, params object[] args)
        {
            Write(ResultColor, format, args);
        }

        public static void WriteStatistic(string format, params object[] args)
        {
            Write(StatisticColor, format, args);
        }

        #endregion

        #region Read

        public static string[] ReadArgsLine()
        {
            return Console.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}