using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;
using System.Linq;
using BenchmarkDotNet.Toolchains.Roslyn;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NuGetReferenceTests : BenchmarkTestExecutor
    {
        public NuGetReferenceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void UserCanSpecifyCustomNuGetPackageDependency()
        {
            var toolchain = RuntimeInformation.IsFullFramework
                ? CsProjClassicNetToolchain.Current.Value // this .NET toolchain will do the right thing, the default RoslynToolchain does not support it
                : CsProjCoreToolchain.Current.Value;

            var job = Job.Dry.With(toolchain).WithNuGet("Newtonsoft.Json", "11.0.2");
            var config = CreateSimpleConfig(job: job);

            CanExecute<WithCallToNewtonsoft>(config);
        }

        [Fact]
        public void RoslynToolchainDoesNotSupportNuGetPackageDependency()
        {
            var toolchain = RoslynToolchain.Instance;

            var unsupportedJob = Job.Dry.With(toolchain).WithNuGet("Newtonsoft.Json", "11.0.2");
            var unsupportedJobConfig = CreateSimpleConfig(job: unsupportedJob);
            var unsupportedJobBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), unsupportedJobConfig);
            var unsupportedJobLogger = new CompositeLogger(unsupportedJobConfig.GetLoggers().ToArray());
            foreach (var benchmarkCase in unsupportedJobBenchmark.BenchmarksCases) 
            {
                Assert.False(toolchain.IsSupported(benchmarkCase, unsupportedJobLogger, BenchmarkRunner.DefaultResolver));
            }

            var supportedJob = Job.Dry.With(toolchain);
            var supportedConfig = CreateSimpleConfig(job: supportedJob);
            var supportedBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToNewtonsoft), supportedConfig);
            var supportedLogger = new CompositeLogger(supportedConfig.GetLoggers().ToArray());
            foreach (var benchmarkCase in supportedBenchmark.BenchmarksCases)
            {
                Assert.True(toolchain.IsSupported(benchmarkCase, supportedLogger, BenchmarkRunner.DefaultResolver));
            }

        }

        public class WithCallToNewtonsoft
        {
            [Benchmark] public void SerializeAnonymousObject() => JsonConvert.SerializeObject(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
        }
    }
}
