using System;
using System.Globalization;

namespace BenchmarkDotNet.Exporters.Csv
{
    public static class CsvSeparatorExtensions
    {
        /// <summary>
        /// Return a string which represent real CSV separator which can be used as plain text.
        /// </summary>
        public static string ToRealSeparator(this CsvSeparator separator)
        {
            switch (separator)
            {
                case CsvSeparator.Comma:
                    return ",";
                case CsvSeparator.Semicolon:
                    return ";";
                case CsvSeparator.CurrentCulture:
                    return CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                default:
                    throw new ArgumentOutOfRangeException(nameof(separator));
            }
        }
    }
}