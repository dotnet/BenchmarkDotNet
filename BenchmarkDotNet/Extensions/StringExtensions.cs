using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class StringExtensions
    {
        private static readonly Lazy<Dictionary<string, string>> InvalidPathCharactersMappings
            = new Lazy<Dictionary<string, string>>(BuildInvalidPathCharactersMappings);

        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }

        /// <summary>
        /// replaces all invalid folder name chars with their number representation
        /// </summary>
        internal static string AsValidPath(this string inputPath)
        {
            var validPathBuilder = new StringBuilder(inputPath);

            foreach (var mapping in InvalidPathCharactersMappings.Value)
            {
                validPathBuilder.Replace(mapping.Key, mapping.Value);
            }

            return validPathBuilder.ToString();
        }

        private static Dictionary<string, string> BuildInvalidPathCharactersMappings()
        {
            return Path.GetInvalidPathChars()
                       .Concat(new[] { '*', '?' }) // also illegal but not listed in Path.GetInvalidPathChars() ?!?!
                       .ToDictionary(
                           character => character.ToString(),
                           character => $"char{(short)character}");
        }
    }
}
