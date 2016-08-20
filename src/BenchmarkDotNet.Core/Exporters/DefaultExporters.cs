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

        public static IExporter Json = JsonExporter.Default;
        public static IExporter JsonBrief = JsonExporter.Brief;
        public static IExporter JsonBriefCompressed = JsonExporter.BriefCompressed;
        public static IExporter JsonFull = JsonExporter.Full;
        public static IExporter JsonFullCompressed = JsonExporter.FullCompressed;
    }
}