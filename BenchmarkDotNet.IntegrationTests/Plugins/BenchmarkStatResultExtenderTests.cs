using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Plugins
{
    public class BenchmarkStatResultExtenderTests
    {
        private readonly ITestOutputHelper output;

        public BenchmarkStatResultExtenderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var extenders = new[]
            {
                BenchmarkStatResultExtender.StdDev,
                BenchmarkStatResultExtender.Min,
                BenchmarkStatResultExtender.Q1,
                BenchmarkStatResultExtender.Median,
                BenchmarkStatResultExtender.Q3,
                BenchmarkStatResultExtender.Max,
                BenchmarkStatResultExtender.OperationPerSecond
            };
            var plugins = BenchmarkPluginBuilder.CreateDefault().
                AddLogger(logger).
                AddResultExtenders(extenders).
                Build();
            var reports = new BenchmarkRunner(plugins).Run<Target>().ToList();
            output.WriteLine(logger.GetLog());

            var table = BenchmarkExporterHelper.BuildTable(reports, plugins.ResultExtenders);
            var headerRow = table.First();
            foreach (var extender in extenders)
                Assert.True(headerRow.Contains(extender.ColumnName));
        }

        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, targetIterationCount: 5)]
        public class Target
        {
            private readonly Random random = new Random(42);

            [Benchmark]
            public void Main50()
            {
                Thread.Sleep(50 + random.Next(50));
            }

            [Benchmark]
            public void Main100()
            {
                Thread.Sleep(100 + random.Next(50));
            }
        }
    }
}