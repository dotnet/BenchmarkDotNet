using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Reports
{
    public class RatioStyleTests
    {
        private readonly ITestOutputHelper output;

        public RatioStyleTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private class TestData
        {
            public RatioStyle RatioStyle { get; }
            public int[] MeanValues { get; }
            public int Noise { get; }
            public string[] ExpectedRatioValues { get; }

            public TestData(RatioStyle ratioStyle, int[] meanValues, int noise, string[] expectedRatioValues)
            {
                RatioStyle = ratioStyle;
                MeanValues = meanValues;
                ExpectedRatioValues = expectedRatioValues;
                Noise = noise;
            }
        }

        private static readonly IDictionary<string, TestData> Data = new Dictionary<string, TestData>
        {
            { "Percentage", new TestData(RatioStyle.Percentage, new[] { 100, 15, 115 }, 1, new[] { "baseline", "-85%", "+15%" }) },
            { "Trend", new TestData(RatioStyle.Trend, new[] { 100, 15, 115 }, 1, new[] { "baseline", "6.83x faster", "1.15x slower" }) }
        };

        [UsedImplicitly]
        public static TheoryData<string> DataNames = TheoryDataHelper.Create(Data.Keys);

        [Theory]
        [MemberData(nameof(DataNames))]
        // First value is baseline, others are benchmark measurements
        public void RatioPrecisionTestWithBaseline([NotNull] string testDataKey)
        {
            var testData = Data[testDataKey];
            var summary = CreateSummary(testData.MeanValues, testData.RatioStyle, testData.Noise);
            int ratioIndex = Array.FindIndex(summary.Table.FullHeader, c => c == BaselineRatioColumn.RatioMean.ColumnName);

            for (int rowIndex = 0; rowIndex < summary.Table.FullContent.Length; rowIndex++)
            {
                var row = summary.Table.FullContent[rowIndex];
                string actual = row[ratioIndex];
                string expected = testData.ExpectedRatioValues[rowIndex];
                Assert.Equal(expected, actual);
            }
        }

        // TODO: Union this with MockFactory
        private Summary CreateSummary(int[] values, RatioStyle ratioStyle, int noise)
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(ratioStyle: ratioStyle));
            var benchmarks = CreateBenchmarks(config).ToList();
            var benchmarkReports = new List<BenchmarkReport>();
            for (var x = 0; x < benchmarks.Count; x++)
            {
                var benchmark = benchmarks[x];
                benchmarkReports.Add(CreateReport(benchmark, values[x], noise));
            }

            var summary = new Summary(
                "MockSummary",
                benchmarkReports.ToImmutableArray(),
                new HostEnvironmentInfoBuilder().Build(),
                string.Empty,
                string.Empty,
                TimeSpan.FromMinutes(1),
                TestCultureInfo.Instance,
                ImmutableArray<ValidationError>.Empty,
                ImmutableArray<IColumnHidingRule>.Empty);
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, int measurementValue, int noise)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, measurementValue),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, measurementValue + noise),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 3, 1, measurementValue - noise),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, measurementValue + 2 * noise),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, measurementValue - 3 * noise)
            };
            var executeResult = new ExecuteResult(measurements, default, default);
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, Array.Empty<Metric>());
        }

        private static IEnumerable<BenchmarkCase> CreateBenchmarks(IConfig config) =>
            BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).BenchmarksCases;

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Baseline() { }

            [Benchmark]
            public void Bar() { }

            [Benchmark]
            public void Foo() { }
        }
    }
}