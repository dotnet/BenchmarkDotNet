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
    public class NativeAotTests : BenchmarkTestExecutor
    {
        public NativeAotTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [FactDotNetCoreOnly("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET Core or NativeAOT process")]
        public void LatestNativeAotVersionIsSupported()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // NativeAOT does not support 32bit yet
                return;
            if (ContinuousIntegration.IsGitHubActionsOnWindows()) // no native dependencies installed
                return;
            if (ContinuousIntegration.IsAppVeyorOnWindows()) // too time consuming for AppVeyor (1h limit)
                return;

            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.GetCurrentVersion())); // we test against latest version for current TFM to make sure we avoid issues like #1055

            CanExecute<NativeAotBenchmark>(config);
        }
    }

    public class NativeAotBenchmark
    {
        [Benchmark]
        public void Check()
        {
            if (!RuntimeInformation.IsNativeAOT)
                throw new Exception("This is NOT NativeAOT");
        }
    }
}