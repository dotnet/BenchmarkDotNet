using System;

namespace BenchmarkDotNet.Portability
{
    internal static class StringExtensions
    {
        internal static bool EqualsWithIgnoreCase(this string left, string right)
        {
#if !CORE
            return left.Equals(right, StringComparison.InvariantCultureIgnoreCase);
#else
            // http://stackoverflow.com/questions/14600694/where-has-stringcomparison-invariantcultureignorecase-gone
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
#endif
        }
    }
}