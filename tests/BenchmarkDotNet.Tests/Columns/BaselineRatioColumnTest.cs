using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Columns
{
    public class BaselineRatioColumnTest
    {
        private readonly ITestOutputHelper output;

        public BaselineRatioColumnTest(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void ColumnsWithBaselineGetsRatio()
        {
            var summary = MockRunner.Run<BaselineRatioColumnBenchmarks>(output, name => name switch
            {
                "BenchmarkSlow" => new double[] { 2, 2, 2 },
                "BenchmarkFast" => new double[] { 4, 4, 4 },
                _ => throw new InvalidOperationException()
            });

            var table = summary.Table;
            string[]? headerRow = table.FullHeader;
            var column = summary.GetColumns()
                .OfType<BaselineRatioColumn>()
                .FirstOrDefault(c => c.Metric == BaselineRatioColumn.RatioMetric.Mean);
            Assert.NotNull(column);

            Assert.Equal(column.ColumnName, headerRow.Last());
            var testNameColumn = Array.FindIndex(headerRow, c => c == "Method");
            var extraColumn = Array.FindIndex(headerRow, c => c == column.ColumnName);
            foreach (var row in table.FullContent)
            {
                Assert.Equal(row.Length, extraColumn + 1);
                if (row[testNameColumn] == "BenchmarkSlow") // This is our baseline
                    Assert.Equal("1.00", row[extraColumn]);
                else if (row[testNameColumn] == "BenchmarkFast") // This should have been compared to the baseline
                    Assert.Contains(".", row[extraColumn]);
            }
        }

        public class BaselineRatioColumnBenchmarks
        {
            [Params(1, 2)]
            public int ParamProperty { get; set; }

            [Benchmark(Baseline = true)]
            public void BenchmarkSlow() => Thread.Sleep(20);

            [Benchmark]
            public void BenchmarkFast() => Thread.Sleep(5);
        }
    }

    public class BaselineRatioResultExtenderNoBaselineTest
    {
        private readonly ITestOutputHelper output;
        public BaselineRatioResultExtenderNoBaselineTest(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void Test()
        {
            var testExporter = new TestExporter();
            var config = new ManualConfig().AddExporter(testExporter);

            MockRunner.Run<BaselineRatioResultExtenderNoBaseline>(output, name => name switch
            {
                "BenchmarkSlow" => new double[] { 2, 2, 2 },
                "BenchmarkFast" => new double[] { 4, 4, 4 },
                _ => throw new InvalidOperationException()
            }, config);

            // Ensure that when the TestBenchmarkExporter() was run, it wasn't passed an instance of "BenchmarkBaselineDeltaResultExtender"
            Assert.False(testExporter.ExportCalled);
            Assert.True(testExporter.ExportToFileCalled);
        }

        public class BaselineRatioResultExtenderNoBaseline
        {
            [Benchmark]
            public void BenchmarkSlow() => Thread.Sleep(50);

            [Benchmark]
            public void BenchmarkFast() => Thread.Sleep(10);
        }

        public class TestExporter : IExporter
        {
            public bool ExportCalled { get; private set; }

            public bool ExportToFileCalled { get; private set; }

            public string Description => "For Testing Only!";

            public string Name => "TestBenchmarkExporter";

            public void ExportToLog(Summary summary, BenchmarkDotNet.Loggers.ILogger logger) => ExportCalled = true;

            public IEnumerable<string> ExportToFiles(Summary summary, BenchmarkDotNet.Loggers.ILogger consoleLogger)
            {
                ExportToFileCalled = true;
                return Enumerable.Empty<string>();
            }
        }
    }

    public class BaselineRatioResultExtenderErrorTest
    {
        private readonly ITestOutputHelper output;
        public BaselineRatioResultExtenderErrorTest(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void OnlyOneMethodCanBeBaseline()
        {
            var summary = MockRunner.Run<BaselineRatioResultExtenderError>(output, name => name switch
            {
                "BenchmarkSlow" => new double[] { 2, 2, 2 },
                "BenchmarkFast" => new double[] { 4, 4, 4 },
                _ => throw new InvalidOperationException()
            });

            // You can't have more than 1 method in a class with [Benchmark(Baseline = true)]
            Assert.True(summary.HasCriticalValidationErrors);
        }

        public class BaselineRatioResultExtenderError
        {
            [Benchmark(Baseline = true)]
            public void BenchmarkSlow() => Thread.Sleep(50);

            [Benchmark(Baseline = true)]
            public void BenchmarkFast() => Thread.Sleep(5);
        }
    }
}