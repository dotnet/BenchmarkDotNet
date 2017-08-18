using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
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
                CreateReports(config),
                MockHostEnvironmentInfo.Default,
                config,
                "",
                TimeSpan.FromMinutes(1),
                Array.Empty<ValidationError>());
        }

        private static BenchmarkReport[] CreateReports(IConfig config)
        {
            return CreateBenchmarks(config).Select(CreateReport).ToArray();
        }

        private static Benchmark[] CreateBenchmarks(IConfig config)
        {
            return BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config);
        }

        private static BenchmarkReport CreateReport(Benchmark benchmark)
        {
            var buildResult = BuildResult.Success(GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>()));
            var executeResult = new ExecuteResult(true, 0, Array.Empty<string>(), Array.Empty<string>());
            var measurements = new List<Measurement>
            {
                new Measurement(1, IterationMode.Result, 1, 1, 1)
            };
            return new BenchmarkReport(benchmark, buildResult, buildResult, new List<ExecuteResult> { executeResult }, measurements, default(GcStats));
        }

        [LongRunJob]
        public class MockBenchmarkClass
        {
            [Benchmark]
            public void Foo()
            {
            }

            [Benchmark]
            public void Bar()
            {
            }
        }

        public class MockHostEnvironmentInfo : HostEnvironmentInfo
        {
            public static MockHostEnvironmentInfo Default = new MockHostEnvironmentInfo
            {
                Architecture = "64mock",
                BenchmarkDotNetVersion = "0.10.x-mock",
                ChronometerFrequency = new Frequency(2531248),
                Configuration = "CONFIGURATION",
                DotNetSdkVersion = new Lazy<string>(() => "1.0.x.mock"),
                HardwareTimerKind = HardwareTimerKind.Tsc,
                HasAttachedDebugger = false,
                HasRyuJit = true,
                IsConcurrentGC = false,
                IsServerGC = false,
                JitInfo = "RyuJIT-v4.6.x.mock",
                JitModules = "clrjit-v4.6.x.mock",
                OsVersion = new Lazy<string>(() => "Microsoft Windows NT 10.0.x.mock"),
                ProcessorCount = 8,
                ProcessorName = new Lazy<string>(() => "MockIntel(R) Core(TM) i7-6700HQ CPU 2.60GHz"),
                RuntimeVersion = "Clr 4.0.x.mock"
            };

            private MockHostEnvironmentInfo()
            {
            }
        }
    }
}