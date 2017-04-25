using System;
using System.Linq;
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
    public class ColumnTests
    {
        private readonly ITestOutputHelper output;

        public ColumnTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void UniqueIdTest()
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(StatisticColumn.Mean)
                .With(StatisticColumn.Mean)
                .With(StatisticColumn.StdDev)
                .With(StatisticColumn.Mean)
                .With(StatisticColumn.Mean)
                .With(StatisticColumn.P67);

            var summary = CreateSummary(config);
            var columns = summary.GetColumns();
            Assert.Equal(1, columns.Count(c => c.Id == StatisticColumn.Mean.Id));
            Assert.Equal(1, columns.Count(c => c.Id == StatisticColumn.StdDev.Id));
            Assert.Equal(1, columns.Count(c => c.Id == StatisticColumn.P67.Id));
        }

        private Summary CreateSummary(IConfig config)
        {
            var logger = new AccumulationLogger();
            var summary = MockFactory.CreateSummary(config);
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

        [Theory]
        [InlineData(4, 0.01)]
        [InlineData(4, 0.123456)]
        [InlineData(4, 0.1)]
        [InlineData(4, 0.0)]
        [InlineData(4, 0.9)]
        [InlineData(3, 1)]
        [InlineData(3, 1.5)]
        [InlineData(3, 9.999999999)]
        [InlineData(2, 10)]
        [InlineData(2, 99.99999999)]
        [InlineData(1, 100)]
        [InlineData(1, 999.9999999)]
        [InlineData(1, 10000)]
        [InlineData(1, 100000)]
        [InlineData(1, double.NaN)]
        [InlineData(1, double.PositiveInfinity)]
        public void BestAmountOfDecimalDigitsTest(int expected, double value)
        {
            Assert.Equal(expected, StatisticColumn.GetBestAmountOfDecimalDigits(value));
        }
    }
}