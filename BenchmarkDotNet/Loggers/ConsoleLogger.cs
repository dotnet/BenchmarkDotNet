using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Loggers
{
    public sealed class ConsoleLogger : ILogger
    {
        public static readonly ILogger Default = new ConsoleLogger();

        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        private readonly Dictionary<LogKind, ConsoleColor> colorScheme;
        private readonly CultureInfo cultureInfo;

        public ConsoleLogger(Dictionary<LogKind, ConsoleColor> colorScheme = null, CultureInfo cultureInfo = null)
        {
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
            this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        public void Write(LogKind logKind, string format, params object[] args)
        {
            Console.ForegroundColor = GetColor(logKind);
            Console.Write(args.Length == 0 ? format : string.Format(cultureInfo, format, args));
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
                { LogKind.Error, ConsoleColor.Red }
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