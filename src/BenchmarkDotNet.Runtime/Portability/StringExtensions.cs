using System;

namespace BenchmarkDotNet.Portability
{
    internal static class StringExtensions
    {
        internal static bool EqualsWithIgnoreCase(this string left, string right)
        {
            // http://stackoverflow.com/questions/14600694/where-has-stringcomparison-invariantcultureignorecase-gone
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}