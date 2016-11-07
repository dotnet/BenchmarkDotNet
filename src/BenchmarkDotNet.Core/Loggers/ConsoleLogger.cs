using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Loggers
{
    public sealed class ConsoleLogger : ILogger
    {
        public static readonly ILogger Default = new ConsoleLogger();

        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        private readonly Dictionary<LogKind, ConsoleColor> colorScheme;

        public ConsoleLogger(Dictionary<LogKind, ConsoleColor> colorScheme = null)
        {
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
        }

        public void Write(LogKind logKind, string text)
        {
            Console.ForegroundColor = GetColor(logKind);
            Console.Write(text);
            Console.ForegroundColor = DefaultColor;
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            Console.ForegroundColor = GetColor(logKind);
            Console.WriteLine(text);
            Console.ForegroundColor = DefaultColor;
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
                { LogKind.Hint, ConsoleColor.DarkBlue }
            };

        public static Dictionary<LogKind, ConsoleColor> CreateGrayScheme()
        {
            var colorScheme = new Dictionary<LogKind, ConsoleColor>();
            foreach (var logKind in Enum.GetValues(typeof(LogKind)).Cast<LogKind>())
                colorScheme[logKind] = ConsoleColor.Gray;
            return colorScheme;
        }
    }
}