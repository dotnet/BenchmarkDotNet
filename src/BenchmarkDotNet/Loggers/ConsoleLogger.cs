using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public sealed class ConsoleLogger : ILogger
    {
        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        public static readonly ILogger Default = new ConsoleLogger();
        public static readonly ILogger Ascii = new ConsoleLogger(false);
        public static readonly ILogger Unicode = new ConsoleLogger(true);

        private readonly bool unicodeSupport;
        private readonly Dictionary<LogKind, ConsoleColor> colorScheme;
        private readonly Dictionary<LogKind, string> logKindAbbreviations;

        private bool newLine = true;

        [PublicAPI]
        public ConsoleLogger(bool unicodeSupport = false, Dictionary<LogKind, ConsoleColor> colorScheme = null)
        {
            this.unicodeSupport = unicodeSupport;
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
            this.logKindAbbreviations = CreateLogKindAbbreviations();
        }

        public string Id => nameof(ConsoleLogger);

        public int Priority => unicodeSupport ? 1 : 0;

        public void Write(LogKind logKind, string text) {
            if (newLine)
                WriteAbbreviation(logKind);
            newLine = false;
            WriteText(Console.Write, text);
        }

        public void WriteLine() {
            Console.WriteLine();
            newLine = true;
        }

        public void WriteLine(LogKind logKind, string text) {
            if (newLine)
                WriteAbbreviation(logKind);
            WriteText(Console.WriteLine, text);
            newLine = true;
        }

        public void Flush() { }

        private void WriteAbbreviation(LogKind logKind)
        {
            var colorBefore = Console.ForegroundColor;

            Console.Write("[");
            try
            {
                var color = GetColor(logKind);
                if (color != Console.ForegroundColor && color != Console.BackgroundColor)
                    Console.ForegroundColor = color;
                var abbreviation = Getabbreviation(logKind);
                Console.Write(abbreviation);
            }
            finally
            {
                if (colorBefore != Console.ForegroundColor && colorBefore != Console.BackgroundColor)
                    Console.ForegroundColor = colorBefore;
            }
            Console.Write("] ");
        }

        private void WriteText(Action<string> write, string text)
        {
            if (!unicodeSupport)
                text = text.ToAscii();

            write(text);
        }

        private ConsoleColor GetColor(LogKind logKind) =>
            colorScheme.ContainsKey(logKind) ? colorScheme[logKind] : DefaultColor;

        private string Getabbreviation(LogKind logKind) =>
            logKindAbbreviations.ContainsKey(logKind) ? logKindAbbreviations[logKind] : "UKN";

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

        private static Dictionary<LogKind, string> CreateLogKindAbbreviations() =>
            new Dictionary<LogKind, string>
            {
                { LogKind.Default, "DEF" },
                { LogKind.Help, "HLP" },
                { LogKind.Header, "HDR" },
                { LogKind.Result, "RES" },
                { LogKind.Statistic, "STA" },
                { LogKind.Info, "INF" },
                { LogKind.Error, "ERR" },
                { LogKind.Hint, "HNT" }
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