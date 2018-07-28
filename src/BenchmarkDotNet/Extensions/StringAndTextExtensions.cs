using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    // Renamed to "StringAndTextExtensions", so it doesn't clash with "StringExtensions" in BenchmarkDotNet.Portability
    public static class StringAndTextExtensions
    {
        private static readonly Lazy<Dictionary<string, string>> InvalidFileNameCharactersMappings
            = new Lazy<Dictionary<string, string>>(BuildInvalidPathCharactersMappings);

        internal static string ToLowerCase(this bool value) => value ? "true" : "false"; // to avoid .ToString().ToLower() allocation

        internal static string Escape(this string path) => "\"" + path + "\"";

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

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
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
                        sb.Append(s[i]);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
