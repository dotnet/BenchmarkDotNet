using System.Xml;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Xml
{
    public abstract class XmlExporterBase : ExporterBase
    {
        protected override string FileExtension => "xml";

        private readonly XmlWriterSettings settings;
        private readonly bool excludeMeasurements;

        public XmlExporterBase(bool indentXml = false, bool excludeMeasurements = false)
        {
            settings = new XmlWriterSettings
            {
                Indent = indentXml,
            };

            this.excludeMeasurements = excludeMeasurements;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            
        }
    }
}
