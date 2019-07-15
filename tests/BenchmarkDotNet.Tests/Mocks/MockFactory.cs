using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockFactory
    {
        public static Summary CreateSummary(Type benchmarkType)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(benchmarkType);
            return new Summary(
                "MockSummary",
                runInfo.BenchmarksCases.Select((benchmark, index) => CreateReport(benchmark, 5, (index + 1) * 100)).ToImmutableArray(),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                string.Empty,
                string.Empty,
                TimeSpan.FromMinutes(1),
                ImmutableArray<ValidationError>.Empty);
        }

        public static Summary CreateSummary(IConfig config) => new Summary(
                "MockSummary",
                CreateReports(config),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                string.Empty,
                string.Empty,
                TimeSpan.FromMinutes(1),
                ImmutableArray<ValidationError>.Empty);

        private static ImmutableArray<BenchmarkReport> CreateReports(IConfig config) 
            => CreateBenchmarks(config).Select(CreateSimpleReport).ToImmutableArray();

        private static BenchmarkCase[] CreateBenchmarks(IConfig config) 
            => BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).BenchmarksCases;

        private static BenchmarkReport CreateSimpleReport(BenchmarkCase benchmarkCase) => CreateReport(benchmarkCase, 1, 1);

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, int n, double nanoseconds)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, default, Array.Empty<string>(), new[] { $"// Runtime=extra output line" });
            var measurements = Enumerable.Range(0, n)
                .Select(index => new Measurement(1, IterationMode.Workload, IterationStage.Result, index + 1, 1, nanoseconds + index))
                .ToList();
            return new BenchmarkReport(true, benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default, Array.Empty<Metric>());
        }

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark] public void Foo() { }

            [Benchmark] public void Bar() { }
        }
    }
}