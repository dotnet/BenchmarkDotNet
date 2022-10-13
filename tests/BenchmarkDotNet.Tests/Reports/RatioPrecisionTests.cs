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
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Reports
{
    public class RatioPrecisionTests
    {
        private readonly ITestOutputHelper output;

        public RatioPrecisionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(new[] { 140, 1, 50 })]
        [InlineData(new[] { 40, 1, 20 })]
        [InlineData(new[] { 0, 1, 20 })]
        // First value is baseline, others are benchmark measurements
        public void RatioPrecisionTestWithBaseline(int[] values)
        {
            var summary = CreateSummary(values);
            int ratioIndex = Array.FindIndex(summary.Table.FullHeader, c => c == BaselineRatioColumn.RatioMean.ColumnName);

            foreach (var row in summary.Table.FullContent)
            {
                ContainsDecimalPointAndCheckDecimalPrecision(values, row[ratioIndex]);
            }
        }

        private void ContainsDecimalPointAndCheckDecimalPrecision(int[] baseLineValues, string value)
        {
            if (value.Contains('.'))
            {
                Assert.Equal((1 / (double)baseLineValues[0]) < 0.01 ? 3 : 2, value.Split('.')[1].Length);
            }
        }

        // TODO: Union this with MockFactory
        private Summary CreateSummary(int[] values)
        {
            var logger = new AccumulationLogger();
            var benchmarks = CreateBenchmarks(DefaultConfig.Instance).ToList();
            var benchmarkReports = new List<BenchmarkReport>();
            for (var x = 0; x < benchmarks.Count; x++)
            {
                var benchmark = benchmarks[x];
                benchmarkReports.Add(CreateReport(benchmark, values[x]));
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

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, int measurementValue)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var measurements = new List<Measurement>
                {
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, measurementValue),
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, measurementValue),
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 3, 1, measurementValue),
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, measurementValue),
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, measurementValue),
                    new Measurement(1, IterationMode.Workload, IterationStage.Result, 6, 1, measurementValue),
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