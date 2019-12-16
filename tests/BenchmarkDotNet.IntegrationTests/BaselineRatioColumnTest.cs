using System;
using System.Linq;
using System.Threading;
using Xunit;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BaselineRatioColumnTest : BenchmarkTestExecutor
    {
        public BaselineRatioColumnTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ColumnsWithBaselineGetsRatio()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            // BenchmarkTestExecutor.CanExecute(..) calls BenchmarkRunner.Run(..) under the hood
            var summary = CanExecute<BaselineRatioColumnBenchmarks>();

            var table = summary.Table;
            var headerRow = table.FullHeader;
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

    public class BaselineRatioResultExtenderNoBaselineTest : BenchmarkTestExecutor
    {
        public BaselineRatioResultExtenderNoBaselineTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var testExporter = new TestExporter();
            var config = CreateSimpleConfig().AddExporter(testExporter);

            CanExecute<BaselineRatioResultExtenderNoBaseline>(config);

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

    public class BaselineRatioResultExtenderErrorTest : BenchmarkTestExecutor
    {
        public BaselineRatioResultExtenderErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void OnlyOneMethodCanBeBaseline()
        {
            var summary = CanExecute<BaselineRatioResultExtenderError>(fullValidation: false);

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

    public class BaselineRatioColumnWithLongParamsTest : BenchmarkTestExecutor
    {
        public BaselineRatioColumnWithLongParamsTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ColumnsWithBaselineGetsRatio()
        {
            var summary = CanExecute<BaselineRatioColumnWithLongParams>(fullValidation: false);

            // Ensure that Params attribute values will not affect Baseline property
            Assert.False(summary.HasCriticalValidationErrors);
        }

        public class BaselineRatioColumnWithLongParams
        {
            // Long different parameters with equal length but different values
            [Params("12345ThisIsALongParameter54321", "12345ThisIsARongParameter54321")]
            public string LongStringParamProperty { get; set; }

            [Benchmark(Baseline = true)]
            public void BenchmarkSlow() => Thread.Sleep(20);

            [Benchmark]
            public void BenchmarkFast() => Thread.Sleep(5);
        }
    }
}