using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Reports
{
    public class SummaryTests
    {
        /// <summary>
        /// Ensures that passing null metrics to BenchmarkReport ctor does not result in NullReferenceException later in Summary ctor.
        /// See also: <see href="https://github.com/dotnet/BenchmarkDotNet/issues/986" />
        /// </summary>
        [Fact]
        public void FailureWithDefaultMetricsDoesNotThrowNre()
        {
            // Arrange:
            IConfig config = DefaultConfig.Instance;
            IEnumerable<BenchmarkCase> benchmarks = CreateBenchmarks(config);
            IList<BenchmarkReport> reports = benchmarks.Select(CreateReportWithDefaultMetrics).ToArray();

            // Act:
            Summary _ = CreateSummary(reports, config);
        }

        private static Summary CreateSummary(IList<BenchmarkReport> reports, IConfig config)
        {
            HostEnvironmentInfo hostEnvironmentInfo = new HostEnvironmentInfoBuilder().Build();
            return new Summary("MockSummary", reports, hostEnvironmentInfo, config, string.Empty, TimeSpan.FromMinutes(1.0), Array.Empty<ValidationError>());
        }

        private static BenchmarkCase[] CreateBenchmarks(IConfig config)
        {
            BenchmarkRunInfo benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config);
            return benchmarkRunInfo.BenchmarksCases;
        }

        private static BenchmarkReport CreateReportWithDefaultMetrics(BenchmarkCase benchmark)
        {
            GenerateResult generateResult = GenerateResult.Failure(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Failure(generateResult);
            // We want to match this call:
            // https://github.com/dotnet/BenchmarkDotNet/blob/89255c9fceb1b27c475a93d08c152349be4199e9/src/BenchmarkDotNet/Running/BenchmarkRunner.cs#L197
            return new BenchmarkReport(false, benchmark, generateResult, buildResult, default, default, default, default);
        }

        [ShortRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }
    }
}
