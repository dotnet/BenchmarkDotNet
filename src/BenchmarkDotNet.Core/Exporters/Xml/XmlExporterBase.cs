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
                Indent = indentXml
            };

            this.excludeMeasurements = excludeMeasurements;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var serializer = new XmlSerializer(typeof(SummaryDto))
                                    .WithRootName(nameof(Summary))
                                    .WithCollectionItemName(typeof(Measurement),
                                                            nameof(Measurement))
                                    .WithCollectionItemName(typeof(BenchmarkReportDto),
                                                            nameof(BenchmarkReport.Benchmark));

            if (excludeMeasurements)
            {
                serializer.WithExcludedProperty(nameof(BenchmarkReportDto.Measurements));
            }

            // Use custom UTF-8 stringwriter because the default writes UTF-16
            StringBuilder builder = new StringBuilder();
            using (var textWriter = new Utf8StringWriter(builder))
            {
                using (var writer = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(writer, new SummaryDto(summary));
                }
            }

            logger.WriteLine(builder.ToString());
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public Utf8StringWriter(StringBuilder builder) :base(builder) { }
    }
}
