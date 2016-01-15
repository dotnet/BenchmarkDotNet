using System;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    public static class StringExtensions
    {
        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }

        public static string ToCamelCase(this string value)
        {
            return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        public static string AddPrefixMultiline(this string str, string prefix)
        {
            var endsWithNewLine = str.EndsWith("\n");
            var res = string.Join("\n", str.Split('\n').Select(line => prefix + line));
            return endsWithNewLine ? res.Remove(res.Length - prefix.Length, prefix.Length) : res;
        }
    }
}
