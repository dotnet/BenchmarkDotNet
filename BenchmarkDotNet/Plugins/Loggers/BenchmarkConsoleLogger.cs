using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Plugins.Loggers
{
    public sealed class BenchmarkConsoleLogger : IBenchmarkLogger
    {
        public static readonly IBenchmarkLogger Default = new BenchmarkConsoleLogger();

        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        private readonly Dictionary<BenchmarkLogKind, ConsoleColor> colorScheme;
        private readonly CultureInfo cultureInfo;

        public BenchmarkConsoleLogger(Dictionary<BenchmarkLogKind, ConsoleColor> colorScheme = null, CultureInfo cultureInfo = null)
        {
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
            this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            Console.ForegroundColor = GetColor(logKind);
            if (args.Length == 0)
                Console.Write(format);
            else
                Console.Write(string.Format(cultureInfo, format, args));
            Console.ForegroundColor = DefaultColor;
        }

        private ConsoleColor GetColor(BenchmarkLogKind logKind)
        {
            return colorScheme.ContainsKey(logKind) ? colorScheme[logKind] : DefaultColor;
        }

        public static Dictionary<BenchmarkLogKind, ConsoleColor> CreateColorfulScheme()
        {
            return new Dictionary<BenchmarkLogKind, ConsoleColor>
            {
                { BenchmarkLogKind.Default, ConsoleColor.Gray },
                { BenchmarkLogKind.Help, ConsoleColor.DarkGreen },
                { BenchmarkLogKind.Header, ConsoleColor.Magenta },
                { BenchmarkLogKind.Result, ConsoleColor.DarkCyan },
                { BenchmarkLogKind.Statistic, ConsoleColor.Cyan },
                { BenchmarkLogKind.Info, ConsoleColor.DarkYellow },
                { BenchmarkLogKind.Error, ConsoleColor.Red }
            };
        }

        public static Dictionary<BenchmarkLogKind, ConsoleColor> CreateGrayScheme()
        {
            var colorScheme = new Dictionary<BenchmarkLogKind, ConsoleColor>();
            foreach (var logKind in Enum.GetValues(typeof(BenchmarkLogKind)).Cast<BenchmarkLogKind>())
                colorScheme[logKind] = ConsoleColor.Gray;
            return colorScheme;
        }
    }
}