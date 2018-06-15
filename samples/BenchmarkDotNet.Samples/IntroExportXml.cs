using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    [XmlExporterAttribute.Brief]
    [XmlExporterAttribute.Full]
    [XmlExporterAttribute.BriefCompressed]
    [XmlExporterAttribute.FullCompressed]
    [XmlExporter("-custom", indentXml: true, excludeMeasurements: true)]
    public class IntroExportXml
    {
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
}
