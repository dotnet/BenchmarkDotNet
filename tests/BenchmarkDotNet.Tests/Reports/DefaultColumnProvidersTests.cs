using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Reports
{
    public class DefaultColumnProvidersTests
    {
        private readonly ITestOutputHelper output;

        public DefaultColumnProvidersTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(false, "Mean, Error, Scaled")]
        [InlineData(true, "Mean, Error, StdDev, Scaled, ScaledSD")]
        public void DefaultStatisticsColumnsTest(bool hugeSd, string expectedColumnNames)
        {
            var summary = CreateSummary(hugeSd);
            var columns = DefaultColumnProviders.Statistics.GetColumns(summary).ToList();
            string columnNames = string.Join(", ", columns.Select(c => c.ColumnName));
            output.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            output.WriteLine("DefaultStatisticsColumns: " + columnNames);
            Assert.Equal(expectedColumnNames, columnNames);
        }

        // TODO: Union this with MockFactory
        private Summary CreateSummary(bool hugeSd)
        {
            var logger = new AccumulationLogger();
            var summary = new Summary(
                "MockSummary",
                CreateBenchmarks(DefaultConfig.Instance).Select(b => CreateReport(b, hugeSd)).ToArray(),
                MockFactory.MockHostEnvironmentInfo.Default,
                DefaultConfig.Instance,
                "",
                TimeSpan.FromMinutes(1),
                Array.Empty<ValidationError>());
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

        private static BenchmarkReport CreateReport(Benchmark benchmark, bool hugeSd)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Result, 1, 1, 1),
                new Measurement(1, IterationMode.Result, 2, 1, hugeSd ? 2 : 1),
                new Measurement(1, IterationMode.Result, 3, 1, 1),
                new Measurement(1, IterationMode.Result, 4, 1, hugeSd ? 2 : 1),
                new Measurement(1, IterationMode.Result, 5, 1, 1),
                new Measurement(1, IterationMode.Result, 6, 1, 1),
            };
            return new BenchmarkReport(benchmark, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default(GcStats));
        }

        private static IEnumerable<Benchmark> CreateBenchmarks(IConfig config) =>
            BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config);


        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }
    }
}