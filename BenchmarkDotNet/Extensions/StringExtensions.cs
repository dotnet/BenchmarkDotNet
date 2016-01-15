using System;

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

    }
}
