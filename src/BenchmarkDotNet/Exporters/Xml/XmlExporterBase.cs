using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Xml
{
    public abstract class XmlExporterBase : ExporterBase
    {
        protected override string FileExtension => "xml";

        private readonly bool indentXml;
        private readonly bool excludeMeasurements;

        protected XmlExporterBase(bool indentXml = false, bool excludeMeasurements = false)
        {
            this.indentXml = indentXml;
            this.excludeMeasurements = excludeMeasurements;
        }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var serializer = BuildSerializer(summary);

            // Use custom UTF-8 StringWriter because the default writes UTF-16
            var stringBuilder = new StringBuilder();
            using (var textWriter = new Utf8StringWriter(stringBuilder))
            {
                using (var writer = new SimpleXmlWriter(textWriter, indentXml))
                {
                    serializer.Serialize(writer, new SummaryDto(summary));
                }
            }

            logger.WriteLine(stringBuilder.ToString());
        }

        private IXmlSerializer BuildSerializer(Summary summary)
        {
            var builder =
                XmlSerializer.GetBuilder(typeof(SummaryDto))
                               .WithRootName(nameof(Summary))
                               .WithCollectionItemName(nameof(BenchmarkReportDto.Measurements),
                                                       nameof(Measurement))
                               .WithCollectionItemName(nameof(SummaryDto.Benchmarks),
                                                       nameof(BenchmarkReport.BenchmarkCase))
                               .WithCollectionItemName(nameof(Statistics.AllOutliers), "Outlier");

            if (!summary.BenchmarksCases.Any(benchmark => benchmark.Config.HasMemoryDiagnoser()))
            {
                builder.WithExcludedProperty(nameof(BenchmarkReportDto.Memory));
            }

            if (excludeMeasurements)
            {
                builder.WithExcludedProperty(nameof(BenchmarkReportDto.Measurements));
            }

            return builder.Build();
        }
    }

    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public Utf8StringWriter(StringBuilder builder) :base(builder) { }
    }
}
