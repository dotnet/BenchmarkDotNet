using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Disassemblers.Exporters;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Helpers;
using BenchmarkDotNet.Tests.Mocks;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Tests.Disassemblers
{
    public class GithubMarkdownDisassemblyExporterMultiCorerunTest
    {
        #region ExportToLog Tests

        /// <summary>
        /// Tests that GithubMarkdownDisassemblyExporter includes Job info in headers.
        /// </summary>
        [Fact]
        public async Task ExportToLog_IncludesJobInfoInHeader()
        {
            // Arrange
            var logger = new AccumulationLogger();
            var config = new DisassemblyDiagnoserConfig();
            var summary = MockFactory.CreateSummary(typeof(MockFactory.MockBenchmarkClass));
            var results = summary.BenchmarksCases.ToImmutableDictionary(
                bc => bc,
                bc => new DisassemblyResult { Methods = [], Errors = [] });
            var exporter = new GithubMarkdownDisassemblyExporter(results, config);

            // Act
            await ((ExporterBase)exporter).ExportToLogAsync(summary, logger, CancellationToken.None);

            // Assert
            var output = logger.GetLog();
            Assert.Contains("(Job:", output);
        }

        [Fact]
        public async Task ExportToLog_FormatsHeaderWithJobDisplayInfo()
        {
            // Arrange
            var logger = new AccumulationLogger();
            var config = new DisassemblyDiagnoserConfig();
            var summary = MockFactory.CreateSummary(typeof(MockFactory.MockBenchmarkClass));
            var results = summary.BenchmarksCases.ToImmutableDictionary(
                bc => bc,
                bc => new DisassemblyResult { Methods = [], Errors = [] });
            var exporter = new GithubMarkdownDisassemblyExporter(results, config);

            // Act
            await ((ExporterBase)exporter).ExportToLogAsync(summary, logger, CancellationToken.None);

            // Assert
            var output = logger.GetLog();
            var lines = output.Split('\n');
            var headers = lines.Where(line => line.StartsWith("##")).ToArray();
            Assert.NotEmpty(headers);
            Assert.All(headers, header => Assert.Contains("(Job:", header));
        }

        #endregion
    }
}
