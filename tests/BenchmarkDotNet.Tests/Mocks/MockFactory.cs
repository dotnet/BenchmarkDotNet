using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
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
                Array.Empty<ValidationError>());
        }

        private static Benchmark CreateBenchmark(IConfig config)
        {
            return BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).First();
        }

        private static BenchmarkReport CreateReport(IConfig config)
        {
            var benchmark = CreateBenchmark(config);
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Result, 1, 1, 1)
            };
            return new BenchmarkReport(benchmark, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default(GcStats));
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