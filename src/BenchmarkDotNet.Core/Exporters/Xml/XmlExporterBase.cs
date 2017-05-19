using System.IO;
using System.Text;
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
            string xml;

            var serializer = new XmlSerializer(typeof(SummaryDto));
            using (var textWriter = new Utf8StringWriter())
            {
                using (var writer = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(writer, new SummaryDto(summary));
                }

                xml = textWriter.ToString();
            }

            logger.WriteLine(xml);
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
