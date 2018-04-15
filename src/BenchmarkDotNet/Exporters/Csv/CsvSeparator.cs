namespace BenchmarkDotNet.Exporters.Csv
{
    public enum CsvSeparator
    {
        /// <summary>
        /// ',' will be used as the CSV separator.
        /// </summary>
        Comma,

        /// <summary>
        /// ';' will be used as the CSV separator.
        /// </summary>
        Semicolon,

        /// <summary>
        ///
        /// </summary>
        CurrentCulture
    }
}