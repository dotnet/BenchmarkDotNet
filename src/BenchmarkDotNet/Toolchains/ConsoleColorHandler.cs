using System;

namespace BenchmarkDotNet.Toolchains
{
    internal class ConsoleColorHandler
    {
        internal static ConsoleColor? ColorBefore;

        public static void SetForegroundColor(ConsoleColor color)
        {
            ColorBefore = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public static void RestoreForegroundColor()
        {
            if (ColorBefore.HasValue)
            {
                Console.ForegroundColor = ColorBefore.Value;
                ColorBefore = null;
            }
        }
    }
}