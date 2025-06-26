using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Portability
{
    internal static class StringExtensions
    {
        internal static bool EqualsWithIgnoreCase(this string left, string right) => left != null && left.Equals(right, StringComparison.InvariantCultureIgnoreCase);

        internal static bool ContainsWithIgnoreCase(this string text, string word) => text != null && text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) >= 0;

        internal static Regex[] ToRegex(this string[] patterns)
            => patterns.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}