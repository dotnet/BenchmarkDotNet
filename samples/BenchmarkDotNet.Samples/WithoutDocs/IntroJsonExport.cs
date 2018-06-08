using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DryJob]
    [JsonExporterAttribute.Brief]
    [JsonExporterAttribute.Full]
    [JsonExporterAttribute.BriefCompressed]
    [JsonExporterAttribute.FullCompressed]
    [JsonExporter("-custom", indentJson: true, excludeMeasurements: true)]
    public class IntroJsonExport
    {
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
}