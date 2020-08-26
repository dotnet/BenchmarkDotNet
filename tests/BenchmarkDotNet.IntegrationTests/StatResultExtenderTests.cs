using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Perfolizer.Horology;
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
            var config = ManualConfig.CreateEmpty().AddJob(CreateJob()).AddLogger(logger).AddColumn(columns);
            var summary = CanExecute<Benchmarks>(config);

            var table = summary.Table;
            var headerRow = table.FullHeader;
            foreach (var column in columns)
                Assert.Contains(column.ColumnName, headerRow);
        }

        private static Job CreateJob() =>
            new Job("MainJob", Job.Dry)
            {
                Run = { IterationCount = 10, IterationTime = TimeInterval.Millisecond * 10 }
            };

        public class Benchmarks
        {
            private readonly Random random = new Random(42);

            [Benchmark]
            public void Sleep5() => Thread.Sleep(5 + random.Next(5));

            [Benchmark]
            public void Sleep10() => Thread.Sleep(10 + random.Next(5));
        }
    }
}