using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Csv
{
    // TODO: Introduce a CsvWriter class (based on ILogger and CsvSeparator)
    public static class CsvHelper
    {
        private const string Quote = "\"";
        private const string TwoQuotes = "\"\"";
        private static readonly char[] ForbiddenSymbols = { '\n', '\r', '"', ',' };

        [NotNull]
        public static string Escape([CanBeNull] string value, string currentListSeparator)
        {
            if (value == null)
                return string.Empty;
            // RFC 4180:
            // 2.6: Fields containing line breaks (CRLF), double quotes, and commas should be enclosed in double-quotes.
            // 2.7: If double-quotes are used to enclose fields, then a double-quote appearing inside a field must be escaped by preceding it with another double quote.
            if (ForbiddenSymbols.Any(value.Contains) || value.Contains(currentListSeparator))
                return Quote + value.Replace(Quote, TwoQuotes) + Quote;
            return value;
        }
    }
}