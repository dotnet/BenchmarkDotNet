using System;
using System.Collections.Generic;
using System.Xml;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Exporters.Xml
{
    public class XmlSerializer
    {
        private readonly Type type;

        public XmlSerializer(Type type)
        {
            this.type = type;
        }

        public void Serialize(XmlWriter writer, SummaryDto summary)
        {
            writer.WriteStartDocument();

            WriteSummary(writer, summary);

            writer.WriteEndDocument();
        }

        private void WriteSummary(XmlWriter writer, SummaryDto summary)
        {
            writer.WriteStartElement(nameof(Summary));
            writer.WriteElementString(nameof(Summary.Title), summary.Title.ToString());

            WriteHostEnvironmentInfo(writer, summary.HostEnvironmentInfo);
            WriteBenchmarks(writer, summary.Benchmarks);

            writer.WriteEndElement();
        }
        
        private void WriteHostEnvironmentInfo(XmlWriter writer, HostEnvironmentInfoDto hostEnvironmentInfo)
        {
            // todo
        }

        private void WriteBenchmarks(XmlWriter writer, IEnumerable<BenchmarkReportDto> benchmarks)
        {
            // todo
        }
    }
}
