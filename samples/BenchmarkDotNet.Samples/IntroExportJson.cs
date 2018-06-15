using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;

namespace BenchmarkDotNet.Samples
{
    // *** Attribute style ***
    
    [DryJob]
    [JsonExporterAttribute.Brief]
    [JsonExporterAttribute.Full]
    [JsonExporterAttribute.BriefCompressed]
    [JsonExporterAttribute.FullCompressed]
    [JsonExporter("-custom", indentJson: true, excludeMeasurements: true)]
    public class IntroExportJson
    {
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
    
    // *** Object style ***
    
    [Config(typeof(Config))]
    public class IntroJsonExportObjectStyle
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(JsonExporter.Brief);
                Add(JsonExporter.Full);
                Add(JsonExporter.BriefCompressed);
                Add(JsonExporter.FullCompressed);
                Add(JsonExporter.Custom("-custom", indentJson: true, excludeMeasurements: true));
            }
        }
        
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
}