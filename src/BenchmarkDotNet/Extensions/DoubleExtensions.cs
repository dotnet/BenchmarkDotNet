using System.Globalization;

namespace BenchmarkDotNet.Extensions
{
    internal static class DoubleExtensions
    {
        public static string ToInvariantString(this double value) => value.ToString(CultureInfo.InvariantCulture);
        public static string ToInvariantString(this double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}