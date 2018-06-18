using System;
using System.Collections.Generic;
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
                runInfo.BenchmarksCases.Select((benchmark, index) => CreateReport(benchmark, 5, (index + 1) * 100)).ToList(),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                runInfo.Config,
                "",
                TimeSpan.FromMinutes(1),
                Array.Empty<ValidationError>());
        }

        public static Summary CreateSummary(IConfig config)
        {
            return new Summary(
                "MockSummary",
                CreateReports(config),
                new HostEnvironmentInfoBuilder().WithoutDotNetSdkVersion().Build(),
                config,
                "",
                TimeSpan.FromMinutes(1),
                Array.Empty<ValidationError>());
        }

        private static BenchmarkReport[] CreateReports(IConfig config)
        {
            return CreateBenchmarks(config).Select(CreateSimpleReport).ToArray();
        }

        private static BenchmarkCase[] CreateBenchmarks(IConfig config)
        {
            return BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).BenchmarksCases;
        }

        private static BenchmarkReport CreateSimpleReport(BenchmarkCase benchmarkCase) => CreateReport(benchmarkCase, 1, 1);

        private static BenchmarkReport CreateReport(BenchmarkCase benchmarkCase, int n, double nanoseconds)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = Enumerable.Range(0, n)
                .Select(index => new Measurement(1, IterationMode.Result, index + 1, 1, nanoseconds + index))
                .ToList();
            return new BenchmarkReport(benchmarkCase, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default);
        }

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark] public void Foo() { }

            [Benchmark] public void Bar() { }
        }
    }
}