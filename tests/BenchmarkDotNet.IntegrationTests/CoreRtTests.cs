using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.CoreRt;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CoreRtTests : BenchmarkTestExecutor
    {
        public CoreRtTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact(Skip = "Disabled until #1606 gets merged with CoreRT toolchain update")]
        //[FactDotNetCoreOnly("It's impossible to reliably detect the version of CoreRT if the process is not a .NET Core or CoreRT process")]
        public void LatestCoreRtVersionIsSupported()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // CoreRT does not support 32bit yet
                return;

            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(CoreRtRuntime.GetCurrentVersion())
                    .WithToolchain(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-*") // we test against latest version to make sure we support latest version and avoid issues like #1055
                        .ToToolchain()));

            CanExecute<CoreRtBenchmark>(config);
        }
    }

    public class CoreRtBenchmark
    {
        [Benchmark]
        public void Check()
        {
            if (!RuntimeInformation.IsCoreRT)
                throw new Exception("This is NOT CoreRT");
        }
    }
}