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
    public class ScaledPrecisionTests
    {
        private readonly ITestOutputHelper output;

        public ScaledPrecisionTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(140)]
        [InlineData(50)]
        public void ScaledPrecisionTestWithBaseline(int baselineValue)
        {
            var summary = CreateSummary(baselineValue);
            var scaledIndex = Array.FindIndex(summary.Table.FullHeader, c => c == "Scaled");
            var testNameColumn = Array.FindIndex(summary.Table.FullHeader, c => c == "Method");
            foreach (var row in summary.Table.FullContent)
            {
                // check precision of scaled column to be 2 or 3 decimal places
                Assert.Equal((1 / (double)baselineValue) < 0.01 ? 3 : 2, row[scaledIndex].Split('.')[1].Length);
            }
        }

        // TODO: Union this with MockFactory
        private Summary CreateSummary(int baselineValue)
        {
            var logger = new AccumulationLogger();
            var summary = new Summary(
                "MockSummary",
                CreateBenchmarks(DefaultConfig.Instance).Select(b => CreateReport(b, baselineValue)).ToArray(),
                MockFactory.MockHostEnvironmentInfo.Default,
                DefaultConfig.Instance,
                "",
                TimeSpan.FromMinutes(1),
                Array.Empty<ValidationError>());
            MarkdownExporter.Default.ExportToLog(summary, logger);
            output.WriteLine(logger.GetLog());
            return summary;
        }

        private static BenchmarkReport CreateReport(Benchmark benchmark, int baselineValue)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = new List<Measurement>();
            if (benchmark.Target.Baseline)
            {
                measurements = new List<Measurement>
                {
                    new Measurement(1, IterationMode.Result, 1, 1, baselineValue),
                    new Measurement(1, IterationMode.Result, 2, 1, baselineValue),
                    new Measurement(1, IterationMode.Result, 3, 1, baselineValue),
                    new Measurement(1, IterationMode.Result, 4, 1, baselineValue),
                    new Measurement(1, IterationMode.Result, 5, 1, baselineValue),
                    new Measurement(1, IterationMode.Result, 6, 1, baselineValue),
                };
            }
            else
            {
                measurements = new List<Measurement>
                {
                    new Measurement(1, IterationMode.Result, 1, 1, 1),
                    new Measurement(1, IterationMode.Result, 2, 1, 1),
                    new Measurement(1, IterationMode.Result, 3, 1, 1),
                    new Measurement(1, IterationMode.Result, 4, 1, 1),
                    new Measurement(1, IterationMode.Result, 5, 1, 1),
                    new Measurement(1, IterationMode.Result, 6, 1, 1),
                };
            }
            return new BenchmarkReport(benchmark, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default(GcStats));
        }

        private static IEnumerable<Benchmark> CreateBenchmarks(IConfig config) =>
            BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).Benchmarks;

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Baseline() { }

            [Benchmark]
            public void Bar() { }
        }
    }
}