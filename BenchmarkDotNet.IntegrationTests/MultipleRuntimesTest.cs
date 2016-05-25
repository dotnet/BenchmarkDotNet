using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MultipleRuntimesTest
    {
        [Fact]
        public void SingleBenchmarkCanBeExecutedForMultpleRuntimes()
        {
            var summary = BenchmarkRunner
                .Run<C>(
                    ManualConfig.CreateEmpty()
                                .With(Job.Dry.With(Runtime.Dnx))
                                .With(Job.Dry.With(Runtime.Core))
                                .With(Job.Dry.With(Runtime.Clr).With(Framework.V46)));

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Clr)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Classic")));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Dnx)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Dnx")));

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
            Console.WriteLine($"{Toolchain.GetToolchain(Runtime.Host)}");
        }
    }
}