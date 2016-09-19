using System.Linq;

namespace BenchmarkDotNet.Exporters.Csv
{
    // TODO: Introduce a CsvWriter class (based on ILogger and CsvSeparator)
    public static class CsvHelper
    {
        private const string Quote = "\"";
        private const string TwoQuotes = "\"\"";
        private static readonly char[] forbiddenSymbols = { '\n', '\r', '"', ',' };

        public static string Escape(string value)
        {
            // RFC 4180:
            // 2.6: Fields containing line breaks (CRLF), double quotes, and commas should be enclosed in double-quotes.
            // 2.7: If double-quotes are used to enclose fields, then a double-quote appearing inside a field must be escaped by preceding it with another double quote.
            if (forbiddenSymbols.Any(value.Contains))
                return Quote + value.Replace(Quote, TwoQuotes) + Quote;
            return value;
        }
    }
}