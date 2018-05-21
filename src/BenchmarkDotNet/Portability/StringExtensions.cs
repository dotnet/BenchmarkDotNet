using System;

namespace BenchmarkDotNet.Portability
{
    internal static class StringExtensions
    {
        internal static bool EqualsWithIgnoreCase(this string left, string right) => left.Equals(right, StringComparison.InvariantCultureIgnoreCase);

        internal static bool ContainsWithIgnoreCase(this string text, string word) => text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) >= 0;
    }
}