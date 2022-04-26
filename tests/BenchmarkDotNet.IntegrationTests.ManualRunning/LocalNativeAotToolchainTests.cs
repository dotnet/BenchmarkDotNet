using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.NativeAot;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    /// <summary>
    /// to run these tests please clone and build NativeAOT first,
    /// then update the hardcoded path
    /// and run following command from console:
    /// dotnet test -c Release -f netcoreapp2.1 --filter "FullyQualifiedName~BenchmarkDotNet.IntegrationTests.ManualRunning.LocalNativeAotToolchainTests"
    ///
    /// in perfect world we would do this OOB for you, but building NativeAOT
    /// so it's not part of our CI jobs
    /// </summary>
    public class LocalNativeAotToolchainTests : BenchmarkTestExecutor
    {
        private const string IlcPath = @"D:\projects\runtime\artifacts\packages\Release\Shipping";

        public LocalNativeAotToolchainTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CanBenchmarkLocalBuildUsingRyuJit()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net60)
                    .WithToolchain(
                        NativeAotToolchain.CreateBuilder()
                            .UseLocalBuild(new System.IO.DirectoryInfo(IlcPath))
                            .ToToolchain()));

            CanExecute<NativeAotBenchmark>(config);
        }
    }

    [KeepBenchmarkFiles]
    public class NativeAotBenchmark
    {
        [Benchmark]
        public void Check()
        {
            if (!string.IsNullOrEmpty(typeof(object).Assembly.Location))
                throw new Exception("This is NOT NativeAOT");
        }
    }
}