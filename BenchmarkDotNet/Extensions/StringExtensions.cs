using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class StringExtensions
    {
        private static readonly Lazy<Dictionary<string, string>> InvalidFileNameCharactersMappings
            = new Lazy<Dictionary<string, string>>(BuildInvalidPathCharactersMappings);

        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }

        /// <summary>
        /// replaces all invalid file name chars with their number representation
        /// </summary>
        internal static string AsValidFileName(this string inputPath)
        {
            var validPathBuilder = new StringBuilder(inputPath);

            foreach (var mapping in InvalidFileNameCharactersMappings.Value)
            {
                validPathBuilder.Replace(mapping.Key, mapping.Value);
            }

            return validPathBuilder.ToString();
        }

        private static Dictionary<string, string> BuildInvalidPathCharactersMappings()
        {
            return Path.GetInvalidFileNameChars()
                       .ToDictionary(
                           character => character.ToString(),
                           character => $"char{(short)character}");
        }
    }
}
