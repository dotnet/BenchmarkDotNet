using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Tests.Mocks
{
    public static class MockFactory
    {
        public static Summary CreateSummary(IConfig config)
        {
            return new Summary(
                "MockSummary",
                new List<BenchmarkReport> { CreateReport(config) },
                HostEnvironmentInfo.GetCurrent(),
                config,
                "",
                TimeSpan.FromMinutes(1),
                new ValidationError[0]);
        }

        private static Benchmark CreateBenchmark(IConfig config)
        {
            return BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).First();
        }

        private static BenchmarkReport CreateReport(IConfig config)
        {
            var benchmark = CreateBenchmark(config);
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty));
            var executeResult = new ExecuteResult(true, 0, new List<string>(), new string[0]);
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Result, 1, 1, 1)
            };
            return new BenchmarkReport(benchmark, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements);
        }

        public class MockBenchmarkClass
        {
            [Benchmark]
            public void Foo()
            {
            }
        }
    }
}