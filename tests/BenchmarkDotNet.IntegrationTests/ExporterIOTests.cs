using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Validators;
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
            var mockSummary = GetMockSummary(resultsDirectoryPath, config: null);
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
            var mockSummary = GetMockSummary(resultsDirectoryPath, config: null);
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

        [Fact]
        public void ExporterUsesFullyQualifiedTypeNameAsFileName()
        {
            string resultsDirectoryPath = Path.GetTempPath();
            var exporter = new MockExporter();
            var mockSummary = GetMockSummary(resultsDirectoryPath, config: null, typeof(Generic<int>));
            var expectedFilePath = $"{Path.Combine(mockSummary.ResultsDirectoryPath, "BenchmarkDotNet.IntegrationTests.Generic_Int32_")}-report.txt";
            string actualFilePath = null;

            try
            {
                actualFilePath = exporter.ExportToFiles(mockSummary, NullLogger.Instance).First();

                Assert.Equal(expectedFilePath, actualFilePath);
            }
            finally
            {
                if (File.Exists(actualFilePath))
                    File.Delete(actualFilePath);
            }
        }

        [Fact]
        public void ExporterUsesSummaryTitleAsFileNameWhenBenchmarksJoinedToSingleSummary()
        {
            string resultsDirectoryPath = Path.GetTempPath();
            var exporter = new MockExporter();
            var joinConfig = ManualConfig.CreateEmpty().WithOptions(ConfigOptions.JoinSummary);
            var mockSummary = GetMockSummary(resultsDirectoryPath, joinConfig, typeof(ClassA), typeof(ClassB));
            var expectedFilePath = $"{Path.Combine(mockSummary.ResultsDirectoryPath, mockSummary.Title)}-report.txt";
            string actualFilePath = null;

            try
            {
                actualFilePath = exporter.ExportToFiles(mockSummary, NullLogger.Instance).First();

                Assert.Equal(expectedFilePath, actualFilePath);
            }
            finally
            {
                if (File.Exists(actualFilePath))
                    File.Delete(actualFilePath);
            }
        }

        private Summary GetMockSummary(string resultsDirectoryPath, IConfig config, params Type[] typesWithBenchmarks)
        {
            return new Summary(
                title: "bdn-test",
                reports: typesWithBenchmarks.Length > 0 ? CreateReports(typesWithBenchmarks, config) : ImmutableArray<BenchmarkReport>.Empty,
                hostEnvironmentInfo: Environments.HostEnvironmentInfo.GetCurrent(),
                resultsDirectoryPath: resultsDirectoryPath,
                logFilePath: string.Empty,
                totalTime: System.TimeSpan.Zero,
                cultureInfo: TestCultureInfo.Instance,
                validationErrors: ImmutableArray<ValidationError>.Empty,
                columnHidingRules: ImmutableArray<IColumnHidingRule>.Empty
            );
        }

        private class MockExporter : ExporterBase
        {
            public int ExportCount;

            public override void ExportToLog(Summary summary, ILogger logger)
            {
                ExportCount++;
            }
        }

        private ImmutableArray<BenchmarkReport> CreateReports(Type[] types, IConfig config = null)
            => CreateBenchmarks(types, config).Select(CreateReport).ToImmutableArray();

        private BenchmarkCase[] CreateBenchmarks(Type[] types, IConfig config)
        {
            return types.SelectMany(type => BenchmarkConverter.TypeToBenchmarks(type, config).BenchmarksCases).ToArray();
        }

        private BenchmarkReport CreateReport(BenchmarkCase benchmarkCase)
        {
            return new BenchmarkReport(success: true,
                                       benchmarkCase: benchmarkCase,
                                       generateResult: null,
                                       buildResult: null,
                                       executeResults: null,
                                       metrics: null);
        }
    }

    public class Generic<T>
    {
        [Benchmark]
        public void Method() { }
    }
}
