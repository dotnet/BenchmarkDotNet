using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class LargeAddressAwareTest
    {
        private readonly ITestOutputHelper output;

        public LargeAddressAwareTest(ITestOutputHelper outputHelper) => output = outputHelper;

        [Fact]
        public void BenchmarkCanAllocateMoreThan2Gb_Core()
        {
            var platform = RuntimeInformation.GetCurrentPlatform();
            var config = ManualConfig.CreateEmpty().WithBuildTimeout(TimeSpan.FromSeconds(240));
            // Running 32-bit benchmarks with .Net Core requires passing the path to 32-bit SDK,
            // which makes this test more complex than it's worth in CI, so we only test 64-bit.
            config.AddJob(Job.Dry.WithRuntime(CoreRuntime.Core80).WithPlatform(platform).WithId(platform.ToString()));
            config.AddColumnProvider(DefaultColumnProviders.Instance)
                  .AddLogger(new OutputLogger(output));

            var summary = BenchmarkRunner.Run<NeedsMoreThan2GB>(config);

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));
            Assert.True(summary.Reports.All(report => report.ExecuteResults.Any()));
            Assert.Equal(1, summary.Reports.Count(report => report.BenchmarkCase.Job.Environment.Runtime is CoreRuntime));

            Assert.Contains(".NET 8.0", summary.AllRuntimes);
        }

        [FactEnvSpecific("Framework is only on Windows", EnvRequirement.WindowsOnly)]
        public void BenchmarkCanAllocateMoreThan2Gb_Framework()
        {
            var platform = RuntimeInformation.GetCurrentPlatform();
            var config = ManualConfig.CreateEmpty();
            // Net481 officially only supports x86, x64, and Arm64.
            config.AddJob(Job.Dry.WithRuntime(ClrRuntime.Net481).WithPlatform(platform).WithGcServer(false).WithLargeAddressAware().WithId(platform.ToString()));
            int jobCount = 1;
            if (platform == Platform.X64)
            {
                ++jobCount;
                config.AddJob(Job.Dry.WithRuntime(ClrRuntime.Net462).WithPlatform(Platform.X86).WithGcServer(false).WithLargeAddressAware().WithId("X86"));
            }
            config.AddColumnProvider(DefaultColumnProviders.Instance)
                  .AddLogger(new OutputLogger(output));

            var summary = BenchmarkRunner.Run<NeedsMoreThan2GB>(config);

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));
            Assert.True(summary.Reports.All(report => report.ExecuteResults.Any()));
            Assert.Equal(jobCount, summary.Reports.Count(report => report.BenchmarkCase.Job.Environment.Runtime is ClrRuntime));

            Assert.Contains(".NET Framework", summary.AllRuntimes);
        }
    }

    public class NeedsMoreThan2GB
    {
        [Benchmark]
        public void AllocateMoreThan2GB()
        {
            Console.WriteLine($"Is64BitProcess = {Environment.Is64BitProcess}");

            const int oneGB = 1024 * 1024 * 1024;
            const int halfGB = oneGB / 2;
            byte[] bytes1 = new byte[oneGB];
            byte[] bytes2 = new byte[oneGB];
            byte[] bytes3 = new byte[halfGB];
            GC.KeepAlive(bytes1);
            GC.KeepAlive(bytes2);
            GC.KeepAlive(bytes3);
        }
    }
}