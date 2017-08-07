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
        /// From http://bryan.reynoldslive.com/post/Wrapping-string-data.aspx
        /// Returns a list of strings no larger than the max length sent in.
        /// </summary>
        /// <remarks>useful function used to wrap string text for reporting.</remarks>
        /// <param name="text">Text to be wrapped into of List of Strings</param>
        /// <param name="maxLength">Max length you want each line to be.</param>
        /// <returns>List of strings</returns>
        internal static List<string> Wrap(string text, int maxLength)
        {
            // Return empty list of strings if the text was empty
            if (text.Length == 0)
                return new List<string>();

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var currentWord in words)
            {
                if ((currentLine.Length > maxLength) ||
                   ((currentLine.Length + currentWord.Length) > maxLength))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                if (currentLine.Length > 0)
                    currentLine += " " + currentWord;
                else
                    currentLine += currentWord;
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine);

            return lines;
        }
    }
}
