using System.Globalization;

namespace BenchmarkDotNet.Helpers
{
    internal static class DefaultCultureInfo
    {
        public static readonly CultureInfo Instance;

        static DefaultCultureInfo()
        {
            Instance = (CultureInfo) CultureInfo.InvariantCulture.Clone();
            Instance.NumberFormat.NumberDecimalSeparator = ".";
        }
    }
}