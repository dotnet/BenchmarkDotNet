using System.Globalization;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.IntegrationTests
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