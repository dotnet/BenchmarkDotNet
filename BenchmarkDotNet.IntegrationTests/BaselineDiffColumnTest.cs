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
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(SingleRunFastConfig))]
    public class BaselineDiffColumnTest
    {
        [Params(1, 2)]
        public int ParamProperty { get; set; }

        [Fact]
        public void Test()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            var summary = BenchmarkRunner.Run(this.GetType());

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
            var logger = new AccumulationLogger();
            var testExporter = new TestExporter();
            var config = DefaultConfig.Instance.With(logger).With(testExporter);
            BenchmarkRunner.Run(this.GetType(), config);

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
    [Config(typeof(SingleRunFastConfig))]
    public class BaselineDeltaResultExtenderErrorTest
    {
        [Fact]
        public void Test()
        {
            // You can't have more than 1 method in a class with [Benchmark(Baseline = true)]
            var summary = BenchmarkRunner.Run(this.GetType());

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

    [Config(typeof(SingleRunFastConfig))]
    public class BaselineDeltaResultExtenderHandlesBenchmarkErrorTest
    {
        [Fact]
        public void Test()
        {
            // This is the common way to run benchmarks, it should wire up the BenchmarkBaselineDeltaResultExtender for us
            var summary = BenchmarkRunner.Run(this.GetType());

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
}
