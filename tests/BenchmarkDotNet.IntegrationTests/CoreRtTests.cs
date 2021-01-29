using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CoreRtTests : BenchmarkTestExecutor
    {
        public CoreRtTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [FactDotNetCoreOnly("It's impossible to reliably detect the version of CoreRT if the process is not a .NET Core or CoreRT process")]
        public void LatestCoreRtVersionIsSupported()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // CoreRT does not support 32bit yet
                return;

            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(CoreRtRuntime.GetCurrentVersion())); // we test against latest version for current TFM to make sure we avoid issues like #1055

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