using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CoreRt;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CoreRtTests : BenchmarkTestExecutor
    {
        public CoreRtTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact]
        public void LatestCoreRtVersionIsSupported()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // CoreRT does not support 32bit yet
                return;
            
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry
                    .With(Runtime.CoreRT)
                    .With(CoreRtToolchain.CreateBuilder()
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