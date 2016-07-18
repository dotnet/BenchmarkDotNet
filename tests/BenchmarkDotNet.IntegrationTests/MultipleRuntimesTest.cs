using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MultipleRuntimesTest
    {
        private readonly ITestOutputHelper output;

        public MultipleRuntimesTest(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact]
        public void SingleBenchmarkCanBeExecutedForMultpleRuntimes()
        {
            var summary = BenchmarkRunner
                .Run<C>(
                    ManualConfig.CreateEmpty()
                                .With(Job.Dry.With(Runtime.Core))
                                .With(Job.Dry.With(Runtime.Clr))
                                .With(DefaultConfig.Instance.GetColumns().ToArray())
                                .With(new OutputLogger(output)));

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Clr)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Classic")));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Core)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Core")));
        }
    }

    // this test was suffering from too long path ex so I had to rename the class and benchmark method to fit within the limit
    public class C
    {
        [Benchmark]
        public void B()
        {
            Console.WriteLine($"{Runtime.Host.GetToolchain()}");
        }
    }
}