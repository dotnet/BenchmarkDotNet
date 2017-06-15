using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ExporterIOTests : BenchmarkTestExecutor
    {
        public ExporterIOTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ExporterWritesToFile()
        {
            string resultsDirectoryPath = Path.GetTempPath();
            var exporter = new MockExporter();
            var mockSummary = GetMockSummary(resultsDirectoryPath);
            var filePath = $"{Path.Combine(mockSummary.ResultsDirectoryPath, mockSummary.Title)}-report.txt"; // ExporterBase default

            try
            {
                exporter.ExportToFiles(mockSummary, NullLogger.Instance);

                Assert.Equal(1, exporter.ExportCount);
                Assert.True(File.Exists(filePath));
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [FactWindowsOnly("On Unix, it's possible to write to an opened file")]
        public void ExporterWorksWhenFileIsLocked()
        {
            string resultsDirectoryPath = Path.GetTempPath();
            var exporter = new MockExporter();
            var mockSummary = GetMockSummary(resultsDirectoryPath);
            var filePath = $"{Path.Combine(mockSummary.ResultsDirectoryPath, mockSummary.Title)}-report.txt"; // ExporterBase default

            try
            {
                exporter.ExportToFiles(mockSummary, NullLogger.Instance);

                Assert.Equal(1, exporter.ExportCount);
                Assert.True(File.Exists(filePath));
                using (var handle = File.OpenRead(filePath)) // Gets a lock on the target file
                {
                    exporter.ExportToFiles(mockSummary, NullLogger.Instance);
                    Assert.Equal(2, exporter.ExportCount);
                }
                var savedFiles = Directory.EnumerateFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*");
                Assert.Equal(2, savedFiles.Count());
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                var otherFiles = Directory.EnumerateFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*");
                foreach (var file in otherFiles)
                    File.Delete(file);
            }
        }

        private Summary GetMockSummary(string resultsDirectoryPath)
        {
            return new Summary(
                title: "bdn-test",
                reports: new List<BenchmarkReport>(),
                hostEnvironmentInfo: Environments.HostEnvironmentInfo.GetCurrent(),
                config: Configs.DefaultConfig.Instance,
                resultsDirectoryPath: resultsDirectoryPath,
                totalTime: System.TimeSpan.Zero,
                validationErrors: new Validators.ValidationError[0]
            );
        }

        private class MockExporter : ExporterBase
        {
            public int ExportCount = 0;

            public override void ExportToLog(Summary summary, ILogger logger)
            {
                ExportCount++;
            }
        }
    }
}
