using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Helpers
{
    internal static class UserInteractionHelper
    {
        /// <summary>
        /// If you are going to show a command example which should be typed by user in a terminal,
        /// all asterisk symbols ('*') should be escaped with the help of quotes
        /// (read more here: <a href="https://www.shellscript.sh/escape.html">https://www.shellscript.sh/escape.html</a>).
        ///
        /// This method escapes such characters on non-Windows platforms.
        ///
        /// </summary>
        /// <remarks>
        /// See also:
        ///   <a href="https://github.com/dotnet/BenchmarkDotNet/issues/842">#842</a>,
        ///   <a href="https://github.com/dotnet/BenchmarkDotNet/issues/1147">#1147</a>
        /// </remarks>
        public static string EscapeCommandExample(string input)
        {
            return !RuntimeInformation.IsWindows() && input.IndexOf('*') >= 0 ? $"'{input}'" : input;
        }
    }
}