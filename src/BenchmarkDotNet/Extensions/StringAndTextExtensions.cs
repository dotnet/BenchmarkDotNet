using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Extensions
{
    // Renamed to "StringAndTextExtensions", so it doesn't clash with "StringExtensions" in BenchmarkDotNet.Portability
    public static class StringAndTextExtensions
    {
        private static readonly Lazy<Dictionary<string, string>> InvalidFileNameCharactersMappings
            = new Lazy<Dictionary<string, string>>(BuildInvalidPathCharactersMappings);

        internal static string ToLowerCase(this bool value) => value ? "true" : "false"; // to avoid .ToString().ToLower() allocation

        // source: https://stackoverflow.com/a/12364234/5852046
        internal static string Escape(this string cliArg)
        {
            if (string.IsNullOrEmpty(cliArg))
                return cliArg;

            string value = Regex.Replace(cliArg, @"(\\*)" + "\"", @"$1\$0");
            value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"", RegexOptions.Singleline);

            return value;
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
            var invalidFileNameChars = Path.GetInvalidFileNameChars().ToList();

            // '\\' is a valid file name char on Unix
            // which can be a problem when we are working with MSBuild
            if (!invalidFileNameChars.Contains('\\'))
                invalidFileNameChars.Add('\\');

            return invalidFileNameChars.ToDictionary(
                character => character.ToString(),
                character => $"char{(short) character}");
        }

        /// <summary>
        /// Returns an HTML encoded string
        /// </summary>
        /// <param name="s">string to encode</param>
        internal static string HtmlEncode(this string s)
        {
            if (s == null)
            {
                return null;
            }

            var sb = new StringBuilder(s.Length);

            foreach (char c in s)
            {
                switch (c)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '\'':
                        sb.Append("&#39;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns file base name
        /// </summary>
        internal static string GetBaseName(this string path, string directory)
        {
            return path.Replace(directory, string.Empty).Trim('/', '\\');
        }
    }
}
