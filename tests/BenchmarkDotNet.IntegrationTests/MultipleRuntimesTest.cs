using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MultipleRuntimesTest
    {
        private readonly ITestOutputHelper output;

        public MultipleRuntimesTest(ITestOutputHelper outputHelper) => output = outputHelper;

        [FactWindowsOnly("CLR is a valid job only on Windows")]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void SingleBenchmarkCanBeExecutedForMultipleRuntimes()
        {
            var summary = BenchmarkRunner
                .Run<C>(
                    ManualConfig.CreateEmpty()
                        .AddJob(Job.Dry.WithRuntime(CoreRuntime.Core60).WithPlatform(Platform.X64).WithId("Core"))
                        .AddJob(Job.Dry.WithRuntime(ClrRuntime.Net462).WithId("Framework"))
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

    // this test was suffering from too long path ex so I had to rename the class and benchmark method to fit within the limit
    public class C
    {
        [Benchmark]
        public void B()
        {
            Console.WriteLine($"// {RuntimeInformation.GetCurrentRuntime().GetToolchain()}");
        }
    }
}