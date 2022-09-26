using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;
using BenchmarkDotNet.Toolchains.Roslyn;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Loggers;
using System.Collections.Immutable;
using BenchmarkDotNet.Tests.XUnit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NuGetReferenceTests : BenchmarkTestExecutor
    {
        public NuGetReferenceTests(ITestOutputHelper output) : base(output) { }

        [FactNotLinux("For some reason this test is unstable on Ubuntu for both AzureDevOps and Travis CI")]
        public void UserCanSpecifyCustomNuGetPackageDependency()
        {
            var toolchain = RuntimeInformation.GetCurrentRuntime().GetToolchain(preferMsBuildToolchains: true);

            var job = Job.Dry.WithToolchain(toolchain).WithNuGet("Newtonsoft.Json", "11.0.2");
            var config = CreateSimpleConfig(job: job);

            CanExecute<WithCallToNewtonsoft>(config);
        }

        [FactClassicDotNetOnly("Roslyn toolchain does not support .NET Core")]
        public void RoslynToolchainDoesNotSupportNuGetPackageDependency()
        {
            var toolchain = RoslynToolchain.Instance;

            var unsupportedJob = Job.Dry.WithToolchain(toolchain).WithNuGet("Newtonsoft.Json", "11.0.2");
            var unsupportedJobConfig = CreateSimpleConfig(job: unsupportedJob);
            var unsupportedJobBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), unsupportedJobConfig);
            var unsupportedJobLogger = new CompositeLogger(unsupportedJobConfig.GetLoggers().ToImmutableHashSet());
            foreach (var benchmarkCase in unsupportedJobBenchmark.BenchmarksCases)
            {
                Assert.False(toolchain.IsSupported(benchmarkCase, unsupportedJobLogger, BenchmarkRunnerClean.DefaultResolver));
            }

            var supportedJob = Job.Dry.WithToolchain(toolchain);
            var supportedConfig = CreateSimpleConfig(job: supportedJob);
            var supportedBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), supportedConfig);
            var supportedLogger = new CompositeLogger(supportedConfig.GetLoggers().ToImmutableHashSet());
            foreach (var benchmarkCase in supportedBenchmark.BenchmarksCases)
            {
                Assert.True(toolchain.IsSupported(benchmarkCase, supportedLogger, BenchmarkRunnerClean.DefaultResolver));
            }
        }

        public class WithCallToNewtonsoft
        {
            [Benchmark] public void SerializeAnonymousObject() => JsonConvert.SerializeObject(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
        }
    }
}
