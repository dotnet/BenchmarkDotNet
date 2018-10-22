using System;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Xml;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class XmlExporterAttribute : ExporterConfigBaseAttribute
    {
        private XmlExporterAttribute(IExporter exporter) : base(exporter)
        {
        }

        public XmlExporterAttribute(string fileNameSuffix = "", bool indentXml = false, bool excludeMeasurements = false)
            : this(new XmlExporter(fileNameSuffix, indentXml, excludeMeasurements))
        {
        }

        public class Brief : XmlExporterAttribute
        {
            public Brief() : base(XmlExporter.Brief)
            {
            }
        }

        public class Full : XmlExporterAttribute
        {
            public Full() : base(XmlExporter.Full)
            {
            }
        }

        public class BriefCompressed : XmlExporterAttribute
        {
            public BriefCompressed() : base(XmlExporter.BriefCompressed)
            {
            }
        }

        public class FullCompressed : XmlExporterAttribute
        {
            public FullCompressed() : base(XmlExporter.FullCompressed)
            {
            }
        }
    }
}
