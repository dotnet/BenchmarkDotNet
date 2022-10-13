using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Tests.Reports
{
    public class SummaryTests
    {
        /// <summary>
        /// Ensures that passing null metrics to BenchmarkReport ctor does not result in NullReferenceException later in Summary ctor.
        /// See also: <see href="https://github.com/dotnet/BenchmarkDotNet/issues/986" />
        /// </summary>
        [Fact]
        public void SummaryWithFailureReportDoesNotThrowNre()
        {
            IList<BenchmarkReport> reports = CreateReports(CreateConfig());

            Assert.NotNull(CreateSummary(reports));
        }

        private static IConfig CreateConfig()
        {
            // We use runtime as selector later. It is chosen as selector just to be close to initial issue. Nothing particularly special about it.
            Job coreJob = new Job(Job.Default).WithRuntime(CoreRuntime.Core20).ApplyAndFreeze(RunMode.Dry);
            Job clrJob = new Job(Job.Default).WithRuntime(ClrRuntime.Net462).ApplyAndFreeze(RunMode.Dry);
            return ManualConfig.Create(DefaultConfig.Instance).AddJob(coreJob).AddJob(clrJob);
        }

        private static BenchmarkReport[] CreateReports(IConfig config)
        {
            BenchmarkRunInfo benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config);
            return benchmarkRunInfo.BenchmarksCases.Select(CreateReport).ToArray();
        }

        private static BenchmarkReport CreateReport(BenchmarkCase benchmark)
        {
            return benchmark.Job.Environment.Runtime is ClrRuntime
                ? CreateFailureReport(benchmark)
                : CreateSuccessReport(benchmark);
        }

        private static BenchmarkReport CreateFailureReport(BenchmarkCase benchmark)
        {
            GenerateResult generateResult = GenerateResult.Failure(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Failure(generateResult, string.Empty);
            // Null may be legitimately passed as metrics to BenchmarkReport ctor here:
            // https://github.com/dotnet/BenchmarkDotNet/blob/89255c9fceb1b27c475a93d08c152349be4199e9/src/BenchmarkDotNet/Running/BenchmarkRunner.cs#L197
            return new BenchmarkReport(false, benchmark, generateResult, buildResult, default, default);
        }

        private static BenchmarkReport CreateSuccessReport(BenchmarkCase benchmark)
        {
            GenerateResult generateResult = GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Success(generateResult);
            var metrics = new[] { new Metric(new FakeMetricDescriptor(), Math.E) };
            return new BenchmarkReport(true, benchmark, generateResult, buildResult, Array.Empty<ExecuteResult>(), metrics);
        }

        private static Summary CreateSummary(IList<BenchmarkReport> reports)
        {
            HostEnvironmentInfo hostEnvironmentInfo = new HostEnvironmentInfoBuilder().Build();
            return new Summary("MockSummary", reports.ToImmutableArray(), hostEnvironmentInfo, string.Empty, string.Empty, TimeSpan.FromMinutes(1.0), TestCultureInfo.Instance, ImmutableArray<ValidationError>.Empty, ImmutableArray<IColumnHidingRule>.Empty);
        }

        public class MockBenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Foo() { }
        }

        private sealed class FakeMetricDescriptor : IMetricDescriptor
        {
            public string Id { get; } = nameof(Id);
            public string DisplayName { get; } = nameof(DisplayName);
            public string Legend { get; } = nameof(Legend);
            public string NumberFormat { get; } = "N";
            public UnitType UnitType { get; }
            public string Unit { get; } = nameof(Unit);
            public bool TheGreaterTheBetter { get; }
            public int PriorityInCategory => 0;
        }
    }
}
