using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [DryJob]
    [XmlExporterAttribute.Brief]
    [XmlExporterAttribute.Full]
    [XmlExporterAttribute.BriefCompressed]
    [XmlExporterAttribute.FullCompressed]
    [XmlExporter("-custom", indentXml: true, excludeMeasurements: true)]
    public class IntroXmlExport
    {
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
}
