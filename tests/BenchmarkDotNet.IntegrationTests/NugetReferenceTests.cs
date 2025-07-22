using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Roslyn;
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
        [Obsolete]
        public void UserCanSpecifyCustomNuGetPackageDependency()
        {
            var toolchain = RuntimeInformation.GetCurrentRuntime().GetToolchain(preferMsBuildToolchains: true);

            const string targetVersion = "9.0.5";

            var job = Job.Dry.WithToolchain(toolchain).WithNuGet("System.Collections.Immutable", targetVersion);
            var config = CreateSimpleConfig(job: job);

            var report = CanExecute<WithCallToImmutableArray>(config);

            // Validate NuGet package version output message
            var stdout = GetSingleStandardOutput(report);
            Assert.Contains($"System.Collections.Immutable: {targetVersion}", stdout);
        }

        [FactEnvSpecific("Roslyn toolchain does not support .NET Core", EnvRequirement.FullFrameworkOnly)]
        [Obsolete]
        public void RoslynToolchainDoesNotSupportNuGetPackageDependency()
        {
            var toolchain = RoslynToolchain.Instance;

            var unsupportedJob = Job.Dry.WithToolchain(toolchain).WithNuGet("System.Collections.Immutable", "9.0.5");
            var unsupportedJobConfig = CreateSimpleConfig(job: unsupportedJob);
            var unsupportedJobBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToImmutableArray), unsupportedJobConfig);

            foreach (var benchmarkCase in unsupportedJobBenchmark.BenchmarksCases)
            {
                Assert.NotEmpty(toolchain.Validate(benchmarkCase, BenchmarkRunnerClean.DefaultResolver));
            }

            var supportedJob = Job.Dry.WithToolchain(toolchain);
            var supportedConfig = CreateSimpleConfig(job: supportedJob);
            var supportedBenchmark = BenchmarkConverter.TypeToBenchmarks(typeof(WithCallToImmutableArray), supportedConfig);
            foreach (var benchmarkCase in supportedBenchmark.BenchmarksCases)
            {
                Assert.Empty(toolchain.Validate(benchmarkCase, BenchmarkRunnerClean.DefaultResolver));
            }
        }

        public class WithCallToImmutableArray
        {
            private readonly double[] values;

            public WithCallToImmutableArray()
            {
                var rand = new Random(Seed: 0);
                values = Enumerable.Range(1, 10_000)
                                   .Select(x => rand.NextDouble())
                                   .ToArray();

                // Gets assembly version text from AssemblyInformationalVersion attribute.
                var version = typeof(ImmutableArray).Assembly
                                                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                                                    .InformationalVersion
                                                    .Split('+')[0];

                // Print referenced NuGet package version to stdout.
                Console.WriteLine($"System.Collections.Immutable: {version}");
            }

            [Benchmark]
            public void ToImmutableArrayBenchmark()
            {
                var results = values.ToImmutableArray();
            }
        }
    }
}