using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Toolchains;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public sealed class ConsoleLogger : ILogger
    {
        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        public static readonly ILogger Default = new ConsoleLogger();

        private readonly Dictionary<LogKind, ConsoleColor> colorScheme;

        public ConsoleLogger(Dictionary<LogKind, ConsoleColor> colorScheme = null)
        {
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
        }

        public void Write(LogKind logKind, string text) => Write(logKind, Console.Write, text);

        public void WriteLine() => Console.WriteLine();

        public void WriteLine(LogKind logKind, string text) => Write(logKind, Console.WriteLine, text);

        private void Write(LogKind logKind, Action<string> write, string text)
        {
            ConsoleHandler.EnsureInitialized(this);

            try
            {
                ConsoleHandler.SetForegroundColor(GetColor(logKind));
                
                write(text);
            }
            finally
            {
                ConsoleHandler.RestoreForegroundColor();
            }
        }

        private ConsoleColor GetColor(LogKind logKind) =>
            colorScheme.ContainsKey(logKind) ? colorScheme[logKind] : DefaultColor;

        private static Dictionary<LogKind, ConsoleColor> CreateColorfulScheme() =>
            new Dictionary<LogKind, ConsoleColor>
            {
                { LogKind.Default, ConsoleColor.Gray },
                { LogKind.Help, ConsoleColor.DarkGreen },
                { LogKind.Header, ConsoleColor.Magenta },
                { LogKind.Result, ConsoleColor.DarkCyan },
                { LogKind.Statistic, ConsoleColor.Cyan },
                { LogKind.Info, ConsoleColor.DarkYellow },
                { LogKind.Error, ConsoleColor.Red },
                { LogKind.Hint, ConsoleColor.DarkCyan }
            };

        [PublicAPI]
        public static Dictionary<LogKind, ConsoleColor> CreateGrayScheme()
        {
            var colorScheme = new Dictionary<LogKind, ConsoleColor>();
            foreach (var logKind in Enum.GetValues(typeof(LogKind)).Cast<LogKind>())
                colorScheme[logKind] = ConsoleColor.Gray;
            return colorScheme;
        }
    }
}