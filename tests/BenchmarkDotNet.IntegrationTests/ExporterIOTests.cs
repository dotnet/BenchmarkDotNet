﻿using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Portability;
using BenchmarkDotNet.Tests.XUnit;
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

        [Fact]
        public void ExporterUsesFullyQualifiedTypeNameAsFileName()
        {
            string resultsDirectoryPath = Path.GetTempPath();
            var exporter = new MockExporter();
            var mockSummary = GetMockSummary(resultsDirectoryPath, typeof(Generic<int>));
            var expectedFilePath = RuntimeInformation.IsWindows()
                ? $"{Path.Combine(mockSummary.ResultsDirectoryPath, "BenchmarkDotNet.IntegrationTests.Generic_Int32_")}-report.txt"
                : $"{Path.Combine(mockSummary.ResultsDirectoryPath, "BenchmarkDotNet.IntegrationTests.Generic<Int32>")}-report.txt"; // "<" is OK for non-Windows OSes ;)
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
            var mockSummary = GetMockSummary(resultsDirectoryPath, typeof(ClassA), typeof(ClassB));
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

        private Summary GetMockSummary(string resultsDirectoryPath, params Type[] typesWithBenchmarks)
        {
            return new Summary(
                title: "bdn-test",
                reports: typesWithBenchmarks.Length > 0 ? CreateReports(typesWithBenchmarks) : Array.Empty<BenchmarkReport>(),
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

        private BenchmarkReport[] CreateReports(Type[] types)
        {
            return CreateBenchmarks(types).Select(CreateReport).ToArray();
        }

        private BenchmarkCase[] CreateBenchmarks(Type[] types)
        {
            return types.SelectMany(type => BenchmarkConverter.TypeToBenchmarks(type).BenchmarksCases).ToArray();
        }

        private BenchmarkReport CreateReport(BenchmarkCase benchmarkCase)
        {
            return new BenchmarkReport(success: true,
                                       benchmarkCase: benchmarkCase,
                                       generateResult: null,
                                       buildResult: null,
                                       executeResults: null,
                                       allMeasurements: null,
                                       gcStats: default,
                                       metrics: null);
        }
    }

    public class Generic<T>
    {
        [Benchmark]
        public void Method() { }
    }
}
