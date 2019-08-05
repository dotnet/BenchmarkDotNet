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
                                .With(new Job(Job.Dry, EnvironmentMode.Core).With(Platform.X64))
                                .With(new Job(Job.Dry, EnvironmentMode.Clr))
                                .With(DefaultColumnProviders.Instance)
                                .With(new OutputLogger(output)));

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
            Assert.Contains(".NET Core", summary.AllRuntimes);
        }

#if NETFRAMEWORK
        [Fact]
        public void ProjectThatTargetsFullDotNetFrameworkOnlyCanNotRunBenchmarksForDotNetCore()
        {
            string expectedErrorMessage = "The project which defines benchmarks targets 'net461', you can not benchmark 'netcoreapp2.1'."
                + Environment.NewLine +
                "To be able to benchmark 'netcoreapp2.1' you need to use <TargetFrameworks>net461;netcoreapp2.1<TargetFrameworks/> in your project file";

            var logger = new OutputLogger(output);

            // this project targets only Full .NET Framework, so trying to benchmark .NET Core is going to fail
            var summary = BenchmarkRunner.Run<SingleRuntime.DotNetFramework.BenchmarkDotNetCore>(
                DefaultConfig.Instance.With(logger));

            Assert.Equal(0, summary.GetNumberOfExecutedBenchmarks());
            Assert.Contains(expectedErrorMessage, logger.GetLog());
        }
#else
        [Fact]
        public void ProjectThatTargetsDotNetCoreOnlyCanNotRunBenchmarksForFullDotNetFramework()
        {
            string expectedErrorMessage = "The project which defines benchmarks targets 'netcoreapp2.1', you can not benchmark 'net461'."
                + Environment.NewLine +
                "To be able to benchmark 'net461' you need to use <TargetFrameworks>netcoreapp2.1;net461<TargetFrameworks/> in your project file";

            var logger = new OutputLogger(output);

            // this project targets only .NET Core, so trying to benchmark Full .NET Framework is going to fail
            var summary = BenchmarkRunner.Run<SingleRuntime.DotNetCore.BenchmarkFullFramework>(
                DefaultConfig.Instance.With(logger));

            Assert.Equal(0, summary.GetNumberOfExecutedBenchmarks());
            Assert.Contains(expectedErrorMessage, logger.GetLog());
        }
#endif
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