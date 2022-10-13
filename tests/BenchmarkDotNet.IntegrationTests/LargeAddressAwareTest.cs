using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
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

        [FactWindowsOnly("CLR is a valid job only on Windows")]
        public void BenchmarkCanAllocateMoreThan2Gb()
        {
            var summary = BenchmarkRunner
                .Run<NeedsMoreThan2GB>(
                    ManualConfig.CreateEmpty()
                        .AddJob(Job.Dry.WithRuntime(CoreRuntime.Core60).WithPlatform(Platform.X64).WithId("Core"))
                        .AddJob(Job.Dry.WithRuntime(ClrRuntime.Net462).WithPlatform(Platform.X86).WithLargeAddressAware().WithId("Framework"))
                        .AddColumnProvider(DefaultColumnProviders.Instance)
                        .AddLogger(new OutputLogger(output)));

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));

            Assert.True(summary.Reports
                .Single(report => report.BenchmarkCase.Job.Environment.Runtime is ClrRuntime)
                .ExecuteResults
                .Any());

            Assert.True(summary.Reports
                .Single(report => report.BenchmarkCase.Job.Environment.Runtime is CoreRuntime)
                .ExecuteResults
                .Any());

            Assert.Contains(".NET Framework", summary.AllRuntimes);
            Assert.Contains(".NET 6.0", summary.AllRuntimes);
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