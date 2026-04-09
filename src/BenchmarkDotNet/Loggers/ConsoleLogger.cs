using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Loggers
{
    public sealed class ConsoleLogger : ILogger
    {
        private const ConsoleColor DefaultColor = ConsoleColor.Gray;

        public static readonly ILogger Default = new ConsoleLogger();
        public static readonly ILogger Ascii = new ConsoleLogger(false);
        public static readonly ILogger Unicode = new ConsoleLogger(true);
        private static readonly Lazy<bool> ConsoleSupportsColors = new(() =>
        {
            if (Environment.GetEnvironmentVariable("NO_COLOR").IsNotBlank())
                return false;

            return !(OsDetector.IsAndroid() || OsDetector.IsIOS() || RuntimeInformation.IsWasm || OsDetector.IsTvOS());
        });

        private static readonly Lazy<bool> ConsoleSupports256Colors = new (() =>
        {
            
            var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
            if ( colorTerm == "truecolor" || colorTerm=="24bit")
                return true;

            var term = Environment.GetEnvironmentVariable("TERM");
            return term!=null && term.Contains("256color");
        });

        private readonly bool unicodeSupport;
        private readonly Dictionary<LogKind, ConsoleColor> colorScheme;

        [PublicAPI]
        public ConsoleLogger(bool unicodeSupport = false, Dictionary<LogKind, ConsoleColor>? colorScheme = null)
        {
            this.unicodeSupport = unicodeSupport;
            this.colorScheme = colorScheme ?? CreateColorfulScheme();
        }

        public string Id => nameof(ConsoleLogger);

        public int Priority => unicodeSupport ? 1 : 0;

        public void Write(LogKind logKind, string text) => Write(logKind, Console.Write, text);

        public void WriteLine() => Console.WriteLine();

        public void WriteLine(LogKind logKind, string text) => Write(logKind, Console.WriteLine, text);

        public void Flush() { }

        private void Write(LogKind logKind, Action<string> write, string text)
        {
            if (!unicodeSupport)
                text = text.ToAscii();

            if (!ConsoleSupportsColors.Value)
            {
                write(text);
                return;
            }

            if (ConsoleSupports256Colors.Value)
            {
                var colorIndex=Get256Color(logKind);
                Console.Write($"\x1b[38;5;{colorIndex}m");
                write(text);
                Console.Write("\x1b[0m");
                return;
            }

            var colorBefore = Console.ForegroundColor;

            try
            {
                var color = GetColor(logKind);
                if (color != Console.ForegroundColor && color != Console.BackgroundColor)
                    Console.ForegroundColor = color;

                write(text);
            }
            finally
            {
                Console.ForegroundColor = colorBefore;
            }
        }

        private ConsoleColor GetColor(LogKind logKind) =>
            colorScheme.ContainsKey(logKind) ? colorScheme[logKind] : DefaultColor;

        private static int Get256Color(LogKind logKind)
        {
            var scheme = Create256ColorScheme();
            return scheme.ContainsKey(logKind) ? scheme[logKind] :252;
        }
        private static Dictionary<LogKind, ConsoleColor> CreateColorfulScheme() =>
    new Dictionary<LogKind, ConsoleColor>
    {
        { LogKind.Default, ConsoleColor.Gray },
        { LogKind.Help, ConsoleColor.Cyan },
        { LogKind.Header, ConsoleColor.Magenta },
        { LogKind.Result, ConsoleColor.Blue },
        { LogKind.Statistic, ConsoleColor.DarkCyan },
        { LogKind.Info, ConsoleColor.DarkGray },
        { LogKind.Error, ConsoleColor.Red },
        { LogKind.Warning, ConsoleColor.DarkYellow },
        { LogKind.Hint, ConsoleColor.DarkMagenta }
    };

private static Dictionary<LogKind, int> Create256ColorScheme() =>
    new Dictionary<LogKind, int>
    {
        { LogKind.Default,   244 },  // mid gray - readable on both
        { LogKind.Help,      36  },  // teal-green
        { LogKind.Header,    127 },  // mid magenta
        { LogKind.Result,    33  },  // mid blue
        { LogKind.Statistic, 30  },  // darker teal - distinct from Result and Help
        { LogKind.Info,      130 },  // dark orange/brown - distinct from Warning
        { LogKind.Error,     160 },  // darker red - visible on white without blinding
        { LogKind.Warning,   166 },  // orange - distinct from Info
        { LogKind.Hint,      98  }   // muted purple - distinct from Header
    };

        [PublicAPI]
        public static Dictionary<LogKind, ConsoleColor> CreateGrayScheme()
        {
            var colorScheme = new Dictionary<LogKind, ConsoleColor>();
            foreach (var logKind in Enum.GetValues<LogKind>())
                colorScheme[logKind] = ConsoleColor.Gray;
            return colorScheme;
        }
    }
}