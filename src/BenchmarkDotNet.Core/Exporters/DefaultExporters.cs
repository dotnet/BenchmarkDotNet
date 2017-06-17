using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;

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

        public static IExporter Xml = XmlExporter.Default;
        public static IExporter XmlBrief = XmlExporter.Brief;
        public static IExporter XmlBriefCompressed = XmlExporter.BriefCompressed;
        public static IExporter XmlFull = XmlExporter.Full;
        public static IExporter XmlFullCompressed = XmlExporter.FullCompressed;
    }
}