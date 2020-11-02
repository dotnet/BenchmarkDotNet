using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using JsonSerializer = SimpleJson.SimpleJson;

namespace BenchmarkDotNet.Tests.Exporters
{
    public class JsonExporterTests
    {
        [Fact]
        public void JsonExporter_ExportToLog_ReturnsValidJson()
        {
            //Arrange
            var logger = new AccumulationLogger();
            var summary = MockFactory.CreateSummary(DefaultConfig.Instance);

            //Act
            JsonExporter.Default.ExportToLog(summary, logger);

            //Assert
            Assert.True(JsonSerializer.TryDeserializeObject(logger.GetLog(), out object obj));
        }
    }
}
