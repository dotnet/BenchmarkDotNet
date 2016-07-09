using BenchmarkDotNet.Exporters.Json;

namespace BenchmarkDotNet.Exporters
{
    public static class DefaultExporters
    {
        public static IExporter AsciiDoc = AsciiDocExporter.Default;
        public static IExporter Csv = CsvExporter.Default;
        public static IExporter CsvMeasurements = CsvMeasurementsExporter.Default;
        public static IExporter Html = HtmlExporter.Default;
        public static IExporter Markdown = MarkdownExporter.Default;
        public static IExporter Plain = PlainExporter.Default;
        public static IExporter RPlot = RPlotExporter.Default;
        public static IExporter BriefJson = BriefJsonExporter.Default;
        public static IExporter FormattedJson = FormattedJsonExporter.Default;
    }


}