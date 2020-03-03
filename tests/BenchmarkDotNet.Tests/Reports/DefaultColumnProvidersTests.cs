using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;
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

        // TODO: Union this with MockFactory
        private Summary CreateSummary(bool hugeSd, Metric[] metrics)
        {
            var logger = new AccumulationLogger();
            var summary = MockFactory.CreateSummary<MockBenchmarkClass>(DefaultConfig.Instance, hugeSd, metrics);
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

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