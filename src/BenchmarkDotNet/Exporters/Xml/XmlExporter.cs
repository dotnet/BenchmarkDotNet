using JetBrains.Annotations;

namespace BenchmarkDotNet.Exporters.Xml
{
    public class XmlExporter : XmlExporterBase
    {
        public static readonly IExporter Brief = new XmlExporter("-brief", indentXml: true, excludeMeasurements: true);
        public static readonly IExporter Full = new XmlExporter("-full", indentXml: true, excludeMeasurements: false);
        public static readonly IExporter BriefCompressed = new XmlExporter("-brief-compressed", indentXml: false, excludeMeasurements: true);
        public static readonly IExporter FullCompressed = new XmlExporter("-full-compressed", indentXml: false, excludeMeasurements: false);

        public static readonly IExporter Default = Brief;

        protected override string FileNameSuffix { get; } = string.Empty;

        public XmlExporter(string fileNameSuffix = "",
                           bool indentXml = false,
                           bool excludeMeasurements = false)
            : base(indentXml, excludeMeasurements)
        {
            FileNameSuffix = fileNameSuffix;
        }

        [PublicAPI]
        public static IExporter Custom(string fileNameSuffix = "",
                                       bool indentXml = false,
                                       bool excludeMeasurements = false) =>
            new XmlExporter(fileNameSuffix, indentXml, excludeMeasurements);
    }
}
