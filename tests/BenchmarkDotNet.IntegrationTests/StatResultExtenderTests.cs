using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class StatResultExtenderTests : BenchmarkTestExecutor
    {
        public StatResultExtenderTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ExtraColumnsCanBeDefined()
        {
            var logger = new OutputLogger(Output);
            var columns = new[]
            {
                StatisticColumn.StdDev,
                StatisticColumn.Min,
                StatisticColumn.Q1,
                StatisticColumn.Median,
                StatisticColumn.Q3,
                StatisticColumn.Max,
                StatisticColumn.OperationsPerSecond,
                StatisticColumn.P85,
                StatisticColumn.P95,
                StatisticColumn.P95
            };
            var config = DefaultConfig.Instance.With(CreateJob()).With(logger).With(columns);
            var summary = CanExecute<Target>(config);

            var table = summary.Table;
            var headerRow = table.FullHeader;
            foreach (var column in columns)
                Assert.True(headerRow.Contains(column.ColumnName));
        }

        private static Job CreateJob() =>
            Job.Dry.
                WithTargetCount(10).
                WithIterationTime(TimeInterval.Millisecond * 10).
                WithId("MainJob");

        public class Target
        {
            private readonly Random random = new Random(42);

            [Benchmark]
            public void Sleep50() => Thread.Sleep(50 + random.Next(50));

            [Benchmark]
            public void Sleep100() => Thread.Sleep(100 + random.Next(50));
        }
    }
}