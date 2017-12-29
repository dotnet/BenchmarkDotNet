using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Portability;
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
        public static Summary CreateSummary(IConfig config)
        {
            return new Summary(
                "MockSummary",
                CreateReports(config),
                new HostEnvironmentInfoBuilder().Build(),
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
            return BenchmarkConverter.TypeToBenchmarks(typeof(MockBenchmarkClass), config).Benchmarks;
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
            public MockHostEnvironmentInfo(
                string architecture, string benchmarkDotNetVersion, Frequency chronometerFrequency, string configuration, string dotNetSdkVersion,
                HardwareTimerKind hardwareTimerKind, bool hasAttachedDebugger, bool hasRyuJit, bool isConcurrentGC, bool isServerGC,
                string jitInfo, string jitModules, string osVersion, int processorCount,
                string processorName, string runtimeVersion, VirtualMachineHypervisor virtualMachineHypervisor)
            {
                Architecture = architecture;
                BenchmarkDotNetVersion = benchmarkDotNetVersion;
                ChronometerFrequency = chronometerFrequency;
                Configuration = configuration;
                DotNetSdkVersion = new Lazy<string>(() => dotNetSdkVersion);
                HardwareTimerKind = hardwareTimerKind;
                HasAttachedDebugger = hasAttachedDebugger;
                HasRyuJit = hasRyuJit;
                IsConcurrentGC = isConcurrentGC;
                IsServerGC = isServerGC;
                JitInfo = jitInfo;
                JitModules = jitModules;
                OsVersion = new Lazy<string>(() => osVersion);
                ProcessorCount = processorCount;
                ProcessorName = new Lazy<string>(() => processorName);
                RuntimeVersion = runtimeVersion;
                VirtualMachineHypervisor = new Lazy<VirtualMachineHypervisor>(() => virtualMachineHypervisor);
            }
        }
    }
}