using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
#if !DEBUG
using Xunit;
#endif
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CustomBuildConfigurationTests : BenchmarkTestExecutor
    {
        public CustomBuildConfigurationTests(ITestOutputHelper output) : base(output)
        {
        }

        [FactEnvSpecific("Flaky, see https://github.com/dotnet/BenchmarkDotNet/issues/2376", EnvRequirement.NonFullFramework)]
        public void UserCanSpecifyCustomBuildConfiguration()
        {
            var jobWithCustomConfiguration = Job.Dry.WithCustomBuildConfiguration("CUSTOM");

            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);

            var report = CanExecute<CustomBuildConfiguration>(config);

#if !DEBUG
            Assert.NotEqual(RuntimeInformation.DebugConfigurationName, report.HostEnvironmentInfo.Configuration);
            Assert.DoesNotContain(report.AllRuntimes, RuntimeInformation.DebugConfigurationName);
#endif
        }

        public class CustomBuildConfiguration
        {
            [Benchmark]
            public void Benchmark()
            {
                if (Assembly.GetEntryAssembly().IsJitOptimizationDisabled().IsTrue())
                {
                    throw new InvalidOperationException("Auto-generated project has not enabled optimizations!");
                }
                if (typeof(CustomBuildConfiguration).Assembly.IsJitOptimizationDisabled().IsTrue())
                {
                    throw new InvalidOperationException("Project that defines benchmarks has not enabled optimizations!");
                }
                if (RuntimeInformation.GetConfiguration() == RuntimeInformation.DebugConfigurationName)
                {
                    throw new InvalidOperationException($"Configuration rezognized as {RuntimeInformation.DebugConfigurationName}!");
                }

#if !CUSTOM
                throw new InvalidOperationException("Should never happen");
#endif
            }
        }
    }
}