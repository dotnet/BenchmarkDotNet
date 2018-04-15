using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CoreRt;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    /// <summary>
    /// to run these tests please clone and build CoreRT first,
    /// then update the hardcoded path
    /// and run following command from console:
    /// dotnet test -c Release -f netcoreapp2.1 --filter "FullyQualifiedName~BenchmarkDotNet.IntegrationTests.ManualRunning.LocalCoreRtToolchainTests"
    /// 
    /// in perfect world we would do this OOB for you, but building CoreRT
    /// so it's not part of our CI jobs
    /// </summary>
    public class LocalCoreRtToolchainTests : BenchmarkTestExecutor
    {
        private const string IlcPath = @"C:\Projects\corert\bin\Windows_NT.x64.Release";

        public LocalCoreRtToolchainTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CanBenchmarkLocalCoreRtUsingRyuJit()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.DryCoreRT.With(
                    CoreRtToolchain.CreateBuilder()
                        .UseCoreRtLocal(IlcPath)
                        .ToToolchain()));

            CanExecute<CoreRtBenchmark>(config);
        }

        [Fact]
        public void CanBenchmarkLocalCoreRtUsingCppCodeGen()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.DryCoreRT.With(
                    CoreRtToolchain.CreateBuilder()
                        .UseCoreRtLocal(IlcPath)
                        .UseCppCodeGenerator() // https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator
                        .ToToolchain()));

            CanExecute<CoreRtBenchmark>(config);
        }
    }

    [KeepBenchmarkFiles()]
    public class CoreRtBenchmark
    {
        [Benchmark]
        public void Check()
        {
            if (!string.IsNullOrEmpty(typeof(object).Assembly.Location))
                throw new Exception("This is NOT CoreRT");
        }
    }
}