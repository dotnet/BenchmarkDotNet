using BenchmarkDotNet.Jobs;
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
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BaselineDiffColumnTest
    {
        [Params(1, 2)]
        public int ParamProperty { get; set; }

        [Fact]
        public void Test()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            // BenchmarkTestExecutor.CanExecute(..) calls BenchmarkRunner.Run(..) under the hood
            var summary = BenchmarkTestExecutor.CanExecute<BaselineDiffColumnTest>();

            var table = summary.Table;
            var headerRow = table.FullHeader;
            var column = summary.Config.GetColumns().OfType<BaselineDiffColumn>().FirstOrDefault();
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

        [Benchmark(Baseline = true)]
        public void BenchmarkSlow() => Thread.Sleep(20);

        [Benchmark]
        public void BenchmarkFast() => Thread.Sleep(5);
    }

    // TODO: repair
    [Config(typeof(SingleRunFastConfig))]
    public class BaselineDeltaResultExtenderNoBaselineTest
    {
        [Fact]
        public void Test()
        {
            var testExporter = new TestExporter();
            var config = DefaultConfig.Instance.With(testExporter);
            BenchmarkTestExecutor.CanExecute<BaselineDeltaResultExtenderNoBaselineTest>(config);

            // Ensure that when the TestBenchmarkExporter() was run, it wasn't passed an instance of "BenchmarkBaselineDeltaResultExtender"
            Assert.False(testExporter.ExportCalled);
            Assert.True(testExporter.ExportToFileCalled);
        }

        [Benchmark]
        public void BenchmarkSlow()
        {
            Thread.Sleep(50);
        }

        [Benchmark]
        public void BenchmarkFast()
        {
            Thread.Sleep(10);
        }

        public class TestExporter : IExporter
        {
            public bool ExportCalled { get; private set; }

            public bool ExportToFileCalled { get; private set; }

            public string Description => "For Testing Only!";

            public string Name => "TestBenchmarkExporter";

            public void ExportToLog(Summary summary, ILogger logger) => ExportCalled = true;

            public IEnumerable<string> ExportToFiles(Summary summary)
            {
                ExportToFileCalled = true;
                return Enumerable.Empty<string>();
            }
        }
    }

    // Todo: use
    public class BaselineDeltaResultExtenderErrorTest
    {
        [Fact]
        public void Test()
        {
            var summary = BenchmarkTestExecutor.CanExecute<BaselineDeltaResultExtenderErrorTest>(fullValidation: false);

            // You can't have more than 1 method in a class with [Benchmark(Baseline = true)]
            Assert.True(summary.HasCriticalValidationErrors);
        }

        [Benchmark(Baseline = true)]
        public void BenchmarkSlow()
        {
            Thread.Sleep(50);
        }

        [Benchmark(Baseline = true)]
        public void BenchmarkFast()
        {
            Thread.Sleep(5);
        }
    }

    public class BaselineDeltaResultExtenderHandlesBenchmarkErrorTest
    {
        [Fact]
        public void Test()
        {
            var summary = BenchmarkTestExecutor.CanExecute<BaselineDeltaResultExtenderHandlesBenchmarkErrorTest>(fullValidation: false);

            var table = summary.Table;
            var headerRow = table.FullHeader;
            var column = summary.Config.GetColumns().OfType<BaselineDiffColumn>().FirstOrDefault();
            Assert.NotNull(column);

            Assert.Equal(column.ColumnName, headerRow.Last());
            var testNameColumn = Array.FindIndex(headerRow, c => c == "Method");
            var extraColumn = Array.FindIndex(headerRow, c => c == column.ColumnName);
            foreach (var row in table.FullContent)
            {
                Assert.Equal(row.Length, extraColumn + 1);
                if (row[testNameColumn] == "BenchmarkSlow") // This is our baseline
                    Assert.Equal("1.00", row[extraColumn]);
                else if (row[testNameColumn] == "BenchmarkThrows") // This should have "?" as it threw an error
                    Assert.Contains("?", row[extraColumn]);
            }
        }

        [Benchmark(Baseline = true)]
        public void BenchmarkSlow()
        {
            Thread.Sleep(50);
        }

        [Benchmark]
        public void BenchmarkThrows()
        {
            // Check that BaselineDiffColumn can handle Benchmarks that throw
            // See https://github.com/PerfDotNet/BenchmarkDotNet/issues/151
            // and https://github.com/PerfDotNet/BenchmarkDotNet/issues/158
            throw new InvalidOperationException("Part of a Unit test - This is expected");
        }
    }

    [Config(typeof(SingleRunFastConfig))]
    public class BaselineScaledColumnsTest
    {
        [Params(1, 2)]
        public int ParamProperty { get; set; }

        [Fact]
        public void Test()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            var config = DefaultConfig.Instance
                .With(Job.Dry.WithTargetCount(5))
                .With(BaselineDiffColumn.Scaled50)
                .With(BaselineDiffColumn.Scaled85)
                .With(BaselineDiffColumn.Scaled95);
            var summary = BenchmarkTestExecutor.CanExecute<BaselineScaledColumnsTest>(config);

            var table = summary.Table;
            var headerRow = table.FullHeader;
            var columns = summary.Config.GetColumns().OfType<BaselineDiffColumn>().ToArray();
            Assert.Equal(columns.Length, 4);

            Assert.Equal(columns[0].ColumnName, headerRow[headerRow.Length - 4]);
            Assert.Equal(columns[1].ColumnName, headerRow[headerRow.Length - 3]);
            Assert.Equal(columns[2].ColumnName, headerRow[headerRow.Length - 2]);
            Assert.Equal(columns[3].ColumnName, headerRow[headerRow.Length - 1]);

            var testNameColumn = Array.FindIndex(headerRow, c => c == "Method");
            var parseCulture = HostEnvironmentInfo.MainCultureInfo;
            foreach (var row in table.FullContent)
            {
                Assert.Equal(row.Length, headerRow.Length);
                if (row[testNameColumn] == "BenchmarkFast") // This is our baseline
                {
                    Assert.Equal("1.00", row[headerRow.Length - 4]); // Scaled
                    Assert.Equal("1.00", row[headerRow.Length - 3]); // S50
                    Assert.Equal("1.00", row[headerRow.Length - 2]); // S85
                    Assert.Equal("1.00", row[headerRow.Length - 1]); // S95
                }
                else if (row[testNameColumn] == "BenchmarkSlow") // This should have been compared to the baseline
                {
                    // This code fails on appveyor
                    // See also: https://github.com/PerfDotNet/BenchmarkDotNet/issues/204
                    //                    var min = 3.0; // 3.7
                    //                    var max = 5.0; // 4.3
                    //                    var scaled = double.Parse(row[headerRow.Length - 4], parseCulture);
                    //                    Assert.InRange(scaled, min, max);
                    //                    var s50 = double.Parse(row[headerRow.Length - 3], parseCulture);
                    //                    Assert.InRange(s50, min, max);
                    //                    var s85 = double.Parse(row[headerRow.Length - 2], parseCulture);
                    //                    Assert.InRange(s85, min, max);
                    //                    var s95 = double.Parse(row[headerRow.Length - 1], parseCulture);
                    //                    Assert.InRange(s95, min, max);
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void BenchmarkFast() => Thread.Sleep(5);

        [Benchmark]
        public void BenchmarkSlow() => Thread.Sleep(20);
    }
}
