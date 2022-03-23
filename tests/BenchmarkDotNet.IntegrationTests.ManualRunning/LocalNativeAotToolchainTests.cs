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
        private const string IlcPath = @"C:\Projects\corert\bin\Windows_NT.x64.Release";

        public LocalNativeAotToolchainTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CanBenchmarkLocalBuildUsingRyuJit()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net50)
                    .WithToolchain(
                        NativeAotToolchain.CreateBuilder()
                            .UseLocalBuild(IlcPath)
                            .ToToolchain()));

            CanExecute<NativeAotBenchmark>(config);
        }

        [Fact]
        public void CanBenchmarkLocalBuildUsingCppCodeGen()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net50)
                    .WithToolchain(
                        NativeAotToolchain.CreateBuilder()
                            .UseLocalBuild(IlcPath)
                            .UseCppCodeGenerator() // https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator
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