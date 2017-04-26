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
    public class BaselineScaledColumnTest : BenchmarkTestExecutor
    {
        public BaselineScaledColumnTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ColumnsWithBaselineGetsScaled()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            // BenchmarkTestExecutor.CanExecute(..) calls BenchmarkRunner.Run(..) under the hood
            var summary = CanExecute<BaselineScaledColumnBenchmarks>();

            var table = summary.Table;
            var headerRow = table.FullHeader;
            var column = summary.GetColumns()
                .OfType<BaselineScaledColumn>()
                .FirstOrDefault(c => c.Kind == BaselineScaledColumn.DiffKind.Mean);
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

        public class BaselineScaledColumnBenchmarks
        {
            [Params(1, 2)]
            public int ParamProperty { get; set; }

            [Benchmark(Baseline = true)]
            public void BenchmarkSlow() => Thread.Sleep(20);

            [Benchmark]
            public void BenchmarkFast() => Thread.Sleep(5);
        }
    }

    public class BaselineScaledResultExtenderNoBaselineTest : BenchmarkTestExecutor
    {
        public BaselineScaledResultExtenderNoBaselineTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Test()
        {
            var testExporter = new TestExporter();
            var config = CreateSimpleConfig().With(testExporter);

            CanExecute<BaselineScaledResultExtenderNoBaseline>(config);

            // Ensure that when the TestBenchmarkExporter() was run, it wasn't passed an instance of "BenchmarkBaselineDeltaResultExtender"
            Assert.False(testExporter.ExportCalled);
            Assert.True(testExporter.ExportToFileCalled);
        }

        public class BaselineScaledResultExtenderNoBaseline
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

    public class BaselineScaledResultExtenderErrorTest : BenchmarkTestExecutor
    {
        public BaselineScaledResultExtenderErrorTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void OnlyOneMethodCanBeBaseline()
        {
            var summary = CanExecute<BaselineScaledResultExtenderError>(fullValidation: false);

            // You can't have more than 1 method in a class with [Benchmark(Baseline = true)]
            Assert.True(summary.HasCriticalValidationErrors);
        }

        public class BaselineScaledResultExtenderError
        {
            [Benchmark(Baseline = true)]
            public void BenchmarkSlow() => Thread.Sleep(50);

            [Benchmark(Baseline = true)]
            public void BenchmarkFast() => Thread.Sleep(5);
        }
    }
}