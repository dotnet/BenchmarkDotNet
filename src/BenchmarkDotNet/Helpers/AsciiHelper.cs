using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    internal static class AsciiHelper
    {
        public static string ToAscii([CanBeNull] this string s)
        {
            return s?.Replace("\u03BC", "u");
        }
    }
}