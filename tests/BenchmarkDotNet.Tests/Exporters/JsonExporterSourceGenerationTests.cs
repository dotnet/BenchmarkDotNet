using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Serialization;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Tests.Reports;
using System.Text.Json;

namespace BenchmarkDotNet.Tests.Exporters
{
    // A Native AOT host serializes the JSON export with source-generated metadata only. That has to cover every type the
    // exporter can produce and give the same output as the reflection-based serialization the verified files pin.
    public class JsonExporterSourceGenerationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SourceGeneratedOnly_SameJsonAsWithReflection(bool excludeMeasurements)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance).AddDiagnoser(Diagnosers.MemoryDiagnoser.Default);
            var summary = MockFactory.CreateSummary(config, hugeSd: false,
                [new Metric(new FakeMetricDescriptor("CacheMisses", "Hardware counter 'CacheMisses' per single operation", "N0"), 7)]);
            var data = new DataProbe(excludeMeasurements).GetData(summary);

            string withReflection = JsonSerializer.Serialize(data, BdnSimpleJsonSerializer.CreateOptions(reflectionFallback: true));
            string sourceGeneratedOnly = JsonSerializer.Serialize(data, BdnSimpleJsonSerializer.CreateOptions(reflectionFallback: false));

            Assert.Contains("\"Memory\":", sourceGeneratedOnly);
            Assert.Contains("\"Statistics\":", sourceGeneratedOnly);
            Assert.Equal(!excludeMeasurements, sourceGeneratedOnly.Contains("\"Measurements\":"));
            Assert.Equal(withReflection, sourceGeneratedOnly);
        }

        private sealed class DataProbe(bool excludeMeasurements) : JsonExporterBase(excludeMeasurements: excludeMeasurements)
        {
            public IReadOnlyDictionary<string, object> GetData(Summary summary) => GetDataToSerialize(summary);
        }
    }
}
