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
                .Run<MultipleRuntimesTest>(
                    ManualConfig.CreateEmpty()
                                .With(Job.Dry.With(Runtime.Dnx))
                                .With(Job.Dry.With(Runtime.Core)));
                                // .With(Job.Dry.With(Runtime.Clr).With(Framework.V40)));

            Assert.True(summary.Reports
                .All(report => report.ExecuteResults
                .All(executeResult => executeResult.FoundExecutable)));

            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));

            // currently disabled due to dotnet cli bug (can not build our .net 4.5 and 4.6 assemblies)
            //Assert.True(summary.Reports
            //    .Single(report => report.Benchmark.Job.Runtime == Runtime.Clr)
            //    .ExecuteResults
            //    .All(executeResult => executeResult.Data.Contains("Classic")));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Dnx)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Dnx")));

            Assert.True(summary.Reports
                .Single(report => report.Benchmark.Job.Runtime == Runtime.Core)
                .ExecuteResults
                .All(executeResult => executeResult.Data.Contains("Core")));
        }

        [Benchmark]
        public void Benchmark()
        {
            Console.WriteLine($"{Toolchain.GetToolchain(Runtime.Host)}");
        }
    }
}