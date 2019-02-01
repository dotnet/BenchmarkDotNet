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
    public class DefaultColumnProvidersTests
    {
        private readonly ITestOutputHelper output;

        public DefaultColumnProvidersTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(false, "Mean, Error, Ratio")]
        [InlineData(true, "Mean, Error, Median, StdDev, Ratio, RatioSD")]
        public void DefaultStatisticsColumnsTest(bool hugeSd, string expectedColumnNames)
        {
            var summary = CreateSummary(hugeSd, Array.Empty<Metric>());
            var columns = DefaultColumnProviders.Statistics.GetColumns(summary).ToList();
            string columnNames = string.Join(", ", columns.Select(c => c.ColumnName));
            output.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            output.WriteLine("DefaultStatisticsColumns: " + columnNames);
            Assert.Equal(expectedColumnNames, columnNames);
        }
        
        [Fact]
        public void EveyMetricHasItsOwnColumn()
        {
            var metrics = new[] { new Metric(new FakeMetricDescriptor("metric1", "some legend"), 0.1), new Metric(new FakeMetricDescriptor("metric2", "another legend"), 0.1) };
            var summary = CreateSummary(false, metrics);

            var columns = DefaultColumnProviders.Metrics.GetColumns(summary).ToArray();
            
            Assert.Equal("metric1", columns[0].Id);
            Assert.Equal("metric2", columns[1].Id);
        }

        private class FakeMetricDescriptor : IMetricDescriptor
        {
            public FakeMetricDescriptor(string id, string legend)
            {
                Id = id;
                Legend = legend;
            }
            
            public string Id { get; }
            public string DisplayName => Id;
            public string Legend { get; }
            public string NumberFormat { get; }
            public UnitType UnitType { get; }
            public string Unit { get; }
            public bool TheGreaterTheBetter { get; }
        }

        // TODO: Union this with MockFactory
        private Summary CreateSummary(bool hugeSd, Metric[] metrics)
        {
            var logger = new AccumulationLogger();
            var summary = new Summary(
                "MockSummary",
                CreateBenchmarks(DefaultConfig.Instance).Select(b => CreateReport(b, hugeSd, metrics)).ToImmutableArray(),
                new HostEnvironmentInfoBuilder().Build(),
                "",
                TimeSpan.FromMinutes(1),
                ImmutableArray<ValidationError>.Empty);
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, bool hugeSd, Metric[] metrics)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            bool isFoo = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Foo";
            bool isBar = benchmarkCase.Descriptor.WorkloadMethodDisplayInfo == "Bar";            
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 2, 1, hugeSd && isFoo ? 2 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 3, 1, hugeSd && isBar ? 3 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 4, 1, hugeSd && isFoo ? 2 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 5, 1, hugeSd && isBar ? 3 : 1),
                new Measurement(1, IterationMode.Workload, IterationStage.Result, 6, 1, 1)
            };
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default, metrics);
        }

        private static IEnumerable<BenchmarkCase> CreateBenchmarks(IConfig config) =>
            BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).BenchmarksCases;


        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }
    }
}