using BenchmarkDotNet.Helpers;
using System.Globalization;

namespace BenchmarkDotNet.Tests
{
    internal static class TestCultureInfo
    {
        public static readonly CultureInfo Instance;

        static TestCultureInfo()
        {
            Instance = (CultureInfo) DefaultCultureInfo.Instance.Clone();
        }
    }
}