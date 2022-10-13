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
        internal static string EscapeCommandLine(this string cliArg)
        {
            if (string.IsNullOrEmpty(cliArg))
                return cliArg;

            string value = Regex.Replace(cliArg, @"(\\*)" + "\"", @"$1\$0");
            value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"", RegexOptions.Singleline);

            return value;
        }

        /// <summary>
        /// Escapes UNICODE control characters
        /// </summary>
        /// <param name="str">string to escape</param>
        /// <param name="quote">True to put (double) quotes around the string literal</param>
        internal static string EscapeSpecialCharacters(this string str, bool quote)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(str, quote);
        }

        /// <summary>
        /// Escapes UNICODE control character
        /// </summary>
        /// <param name="c">char to escape</param>
        /// <param name="quote">True to put (single) quotes around the character literal.</param>
        internal static string EscapeSpecialCharacter(this char c, bool quote)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote);
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

        /// <summary>
        /// Standardizes the whitespace before/after arguments so that all arguments are separated by a single space
        /// </summary>
        /// <param name="stringBuilder">The string builder that will hold the arguments</param>
        /// <param name="argument">The argument to append to this string builder</param>
        /// <returns>The string builder with the arguments added</returns>
        internal static StringBuilder AppendArgument(this StringBuilder stringBuilder, string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return stringBuilder;
            }
            argument = " " + argument.Trim();
            stringBuilder.Append(argument);

            return stringBuilder;
        }

        /// <summary>
        /// Standardizes the whitespace before/after arguments so that all arguments are separated by a single space
        /// </summary>
        /// <param name="stringBuilder">The string builder that will hold the arguments</param>
        /// <param name="argument">The argument to append to this string builder</param>
        /// <returns>The string builder with the arguments added</returns>
        internal static StringBuilder AppendArgument(this StringBuilder stringBuilder, object argument)
            => argument == null ? stringBuilder : AppendArgument(stringBuilder, argument.ToString());
    }
}
