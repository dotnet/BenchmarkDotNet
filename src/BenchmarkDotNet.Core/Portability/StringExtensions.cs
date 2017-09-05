using System;

namespace BenchmarkDotNet.Portability
{
    internal static class StringExtensions
    {
        internal static bool EqualsWithIgnoreCase(this string left, string right)
        {
            return left.Equals(right, IgnoreCaseStringComparison);
        }

        internal static bool ContainsWithIgnoreCase(this string text, string word)
        {
            return text.IndexOf(word, IgnoreCaseStringComparison) >= 0;
        }

        private static StringComparison IgnoreCaseStringComparison
        {
            get
            {
#if !NETCOREAPP1_1
                return StringComparison.InvariantCultureIgnoreCase;
#else
// http://stackoverflow.com/questions/14600694/where-has-stringcomparison-invariantcultureignorecase-gone
                return StringComparison.OrdinalIgnoreCase;
#endif
            }
        }
    }
}