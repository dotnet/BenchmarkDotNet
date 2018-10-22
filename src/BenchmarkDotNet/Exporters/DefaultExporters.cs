using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters
{
    public static class DefaultExporters
    {
        [PublicAPI] public static readonly IExporter AsciiDoc = AsciiDocExporter.Default;
        [PublicAPI] public static readonly IExporter Csv = CsvExporter.Default;
        [PublicAPI] public static readonly IExporter CsvMeasurements = CsvMeasurementsExporter.Default;
        [PublicAPI] public static readonly IExporter Html = HtmlExporter.Default;
        [PublicAPI] public static readonly IExporter Markdown = MarkdownExporter.Default;
        [PublicAPI] public static readonly IExporter Plain = PlainExporter.Default;
        [PublicAPI] public static readonly IExporter RPlot = RPlotExporter.Default;

        [PublicAPI] public static readonly IExporter Json = JsonExporter.Default;
        [PublicAPI] public static readonly IExporter JsonBrief = JsonExporter.Brief;
        [PublicAPI] public static readonly IExporter JsonBriefCompressed = JsonExporter.BriefCompressed;
        [PublicAPI] public static readonly IExporter JsonFull = JsonExporter.Full;
        [PublicAPI] public static readonly IExporter JsonFullCompressed = JsonExporter.FullCompressed;

        [PublicAPI] public static readonly IExporter Xml = XmlExporter.Default;
        [PublicAPI] public static readonly IExporter XmlBrief = XmlExporter.Brief;
        [PublicAPI] public static readonly IExporter XmlBriefCompressed = XmlExporter.BriefCompressed;
        [PublicAPI] public static readonly IExporter XmlFull = XmlExporter.Full;
        [PublicAPI] public static readonly IExporter XmlFullCompressed = XmlExporter.FullCompressed;
    }
}