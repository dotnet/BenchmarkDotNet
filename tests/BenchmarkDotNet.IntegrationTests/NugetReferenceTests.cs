using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Roslyn;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NuGetReferenceTests : BenchmarkTestExecutor
    {
        public NuGetReferenceTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void UserCanSpecifyCustomNuGetPackageDependency()
        {
            var toolchain = RuntimeInformation.GetCurrentRuntime().GetToolchain(preferMsBuildToolchains: true);

            var job = Job.Dry.WithToolchain(toolchain).WithNuGet("Newtonsoft.Json", "13.0.2");
            var config = CreateSimpleConfig(job: job);

            CanExecute<WithCallToNewtonsoft>(config);
        }

        [FactEnvSpecific("Roslyn toolchain does not support .NET Core", EnvRequirement.FullFrameworkOnly)]
        public void RoslynToolchainDoesNotSupportNuGetPackageDependency()
        {
            var toolchain = RoslynToolchain.Instance;

            var unsupportedJob = Job.Dry.WithToolchain(toolchain).WithNuGet("Newtonsoft.Json", "13.0.2");
            var unsupportedJobConfig = CreateSimpleConfig(job: unsupportedJob);
            var unsupportedJobBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), unsupportedJobConfig);

            foreach (var benchmarkCase in unsupportedJobBenchmark.BenchmarksCases)
            {
                Assert.NotEmpty(toolchain.Validate(benchmarkCase, BenchmarkRunnerClean.DefaultResolver));
            }

            var supportedJob = Job.Dry.WithToolchain(toolchain);
            var supportedConfig = CreateSimpleConfig(job: supportedJob);
            var supportedBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), supportedConfig);
            foreach (var benchmarkCase in supportedBenchmark.BenchmarksCases)
            {
                Assert.Empty(toolchain.Validate(benchmarkCase, BenchmarkRunnerClean.DefaultResolver));
            }
        }

        public class WithCallToNewtonsoft
        {
            [Benchmark] public void SerializeAnonymousObject() => JsonConvert.SerializeObject(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
        }
    }
}