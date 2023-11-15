using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.NativeAot;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BuildTimeoutTests: BenchmarkTestExecutor
    {
        public BuildTimeoutTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact]
        public void WhenBuildTakesMoreTimeThanTheTimeoutTheBuildIsCancelled()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // NativeAOT does not support 32bit yet
                return;
            if (RuntimeInformation.IsMacOS())
                return; // currently not supported

            // we use NativeAOT on purpose because it takes a LOT of time to build it
            // so we can be sure that timeout = 1s should fail!
            var timeout = TimeSpan.FromSeconds(1);

            var config = ManualConfig.CreateEmpty()
                .WithBuildTimeout(timeout)
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net80)
                    .WithToolchain(NativeAotToolchain.CreateBuilder()
                        .UseNuGet("8.0.0", "https://api.nuget.org/v3/index.json")
                        .TargetFrameworkMoniker("net8.0")
                        .ToToolchain()));

            var summary = CanExecute<NativeAotBenchmark>(config, fullValidation: false);

            Assert.All(summary.Reports, report => Assert.False(report.BuildResult.IsBuildSuccess));
            Assert.All(summary.Reports, report => Assert.Contains("The configured timeout", report.BuildResult.ErrorMessage));
        }
    }

    public class Impossible
    {
        [Benchmark]
        public void Check() => Environment.FailFast("This benchmark should have been never executed because 1s is not enough to build NativeAOT benchmark!");
    }
}
