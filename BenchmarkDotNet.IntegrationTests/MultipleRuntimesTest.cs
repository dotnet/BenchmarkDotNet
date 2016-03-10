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
        internal class AllRuntimesConfig : ManualConfig
        {
            public AllRuntimesConfig()
            {
                Add(Job.Dry.With(Runtime.Dnx).With(Jit.Host));
                Add(Job.Dry.With(Runtime.Core).With(Jit.Host));
                Add(Job.Dry.With(Runtime.Clr).With(Jit.Host).With(Framework.V45));
            }
        }

        [Fact]
        public void SingleBenchmarkCanBeExecutedForMultpleRuntimes()
        {
            var summary = BenchmarkRunner.Run<MultipleRuntimesTest>(new AllRuntimesConfig());

            Assert.True(summary.Reports.Values.All(report => report.ExecuteResults.All(executeResult => executeResult.FoundExecutable)));
            Assert.True(summary.Reports.Values.All(report => report.AllMeasurements.Any()));
            Assert.True(summary.Reports.Values.Any(report => report.ExecuteResults.Any(executeResult => executeResult.Data.Contains("Classic"))));
            Assert.True(summary.Reports.Values.Any(report => report.ExecuteResults.Any(executeResult => executeResult.Data.Contains("Dnx"))));
            Assert.True(summary.Reports.Values.Any(report => report.ExecuteResults.Any(executeResult => executeResult.Data.Contains("Core"))));
        }

        [Benchmark]
        public void Benchmark()
        {
            Console.WriteLine($"{Toolchain.GetToolchain(Runtime.Host)}");
        }
    }
}