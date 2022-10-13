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

            // we use NativeAOT on purpose because it takes a LOT of time to build it
            // so we can be sure that timeout = 1s should fail!
            var timeout = TimeSpan.FromSeconds(1);

            var config = ManualConfig.CreateEmpty()
                .WithBuildTimeout(timeout)
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net60)
                    .WithToolchain(NativeAotToolchain.CreateBuilder()
                        .UseNuGet(
                            "6.0.0-rc.1.21420.1", // we test against specific version to keep this test stable
                            "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json") // using old feed that supports net6.0
                        .TargetFrameworkMoniker("net6.0")
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
