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
        public void CoreRtIsSupported()
        {
            var config = ManualConfig.CreateEmpty()
                .With(Job.Dry
                    .With(Runtime.CoreRT)
                    .With(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-26414-01") // we test against specific version to keep this test stable
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