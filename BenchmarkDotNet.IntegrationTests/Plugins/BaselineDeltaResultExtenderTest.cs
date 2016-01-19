using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Tasks;
using System;
using System.Linq;
using System.Threading;
using Xunit;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests.Plugins
{
    public class BaselineDeltaResultExtenderTest
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public int ParamProperty { get; set; }

        [Fact]
        public void Test()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            var runner = new BenchmarkRunner();
            var reports = runner.Run<BaselineDeltaResultExtenderTest>().ToList();

            var table = BenchmarkExporterHelper.BuildTable(reports, runner.Plugins.ResultExtenders);
            var headerRow = table.First();
            var extender = runner.Plugins.ResultExtenders.OfType<BenchmarkBaselineDeltaResultExtender>().FirstOrDefault();
            Assert.NotNull(extender);

            Assert.Equal(extender.ColumnName, headerRow.Last());
            var testNameColumn = Array.FindIndex(headerRow, c => c == "Method");
            var extraColumn = Array.FindIndex(headerRow, c => c == extender.ColumnName);
            foreach (var row in table)
            {
                Assert.Equal(row.Length, extraColumn + 1);
                if (row[testNameColumn] == "BenchmarkSlow") // This is our baseline
                    Assert.Equal(row[extraColumn], "-");
                else if (row[testNameColumn] == "BenchmarkFast") // This should have been compared to the baseline
                    Assert.Contains("%", row[extraColumn]);
            }
        }

        [Benchmark(Baseline = true)]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkSlow()
        {
            Thread.Sleep(100);
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkFast()
        {
            Thread.Sleep(5);
        }
    }

    public class BaselineDeltaResultExtenderNoBaselineTest
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var testExporter = new TestBenchmarkExporter();
            var plugins = BenchmarkPluginBuilder.CreateDefault()
                                .AddLogger(logger)
                                .AddExporters(testExporter)
                                .Build();
            var reports = new BenchmarkRunner(plugins).Run(this.GetType()).ToList();

            // Ensure that when the TestBenchmarkExporter() was run, it wasn't passed an instance of "BenchmarkBaselineDeltaResultExtender"
            Assert.False(testExporter.ExportCalled);
            Assert.Null(testExporter.ExportResultExtenders);
            Assert.NotNull(testExporter.ExportToFileResultExtenders);
            Assert.Equal(0, testExporter.ExportToFileResultExtenders.OfType<BenchmarkBaselineDeltaResultExtender>().Count());
            Assert.True(testExporter.ExportToFileCalled);
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkSlow()
        {
            Thread.Sleep(100);
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkFast()
        {
            Thread.Sleep(5);
        }

        public class TestBenchmarkExporter : IBenchmarkExporter
        {
            public IEnumerable<IBenchmarkResultExtender> ExportResultExtenders { get; private set; }
            public bool ExportCalled { get; private set; }

            public IEnumerable<IBenchmarkResultExtender> ExportToFileResultExtenders { get; private set; }
            public bool ExportToFileCalled { get; private set; }

            public string Description
            {
                get { return "For Testing Only!"; }
            }

            public string Name
            {
                get { return "TestBenchmarkExporter"; }
            }

            public void Export(IList<BenchmarkReport> reports, IBenchmarkLogger logger, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
            {
                ExportResultExtenders = resultExtenders;
                ExportCalled = true;
            }

            public IEnumerable<string> ExportToFile(IList<BenchmarkReport> reports, string competitionName, IEnumerable<IBenchmarkResultExtender> resultExtenders = null)
            {
                ExportToFileResultExtenders = resultExtenders;
                ExportToFileCalled = true;
                return Enumerable.Empty<string>();
            }
        }
    }

    public class BaselineDeltaResultExtenderErrorTest
    {
        [Fact]
        public void Test()
        {
            // You can't have more than 1 method in a class with [Benchmark(Baseline = true)]
            Assert.Throws<InvalidOperationException>(() => new BenchmarkRunner().Run(this.GetType()));
        }

        [Benchmark(Baseline = true)]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkSlow()
        {
            Thread.Sleep(100);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkFast()
        {
            Thread.Sleep(5);
        }
    }
}
