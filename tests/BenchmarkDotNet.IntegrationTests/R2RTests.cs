using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.R2R;

namespace BenchmarkDotNet.IntegrationTests
{
    public class R2RTests : BenchmarkTestExecutor
    {
        public R2RTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("R2R requires .NET Core runtime", EnvRequirement.DotNetCoreOnly, EnvRequirement.NonGitHubDraftPR)]
        public void R2RToolchainCanExecuteBenchmarks()
        {
            try
            {
                var config = ManualConfig.CreateEmpty()
                    .AddJob(Job.Dry
                        .WithToolchain(CsProjR2RToolchain.R2R10_0))
                    .WithBuildTimeout(TimeSpan.FromSeconds(360));
                var summary = CanExecute<R2RBenchmark>(config);

                Assert.True(summary.Reports.Length > 0, "Expected at least one benchmark report");
                Assert.True(summary.Reports[0].Success, "Benchmark should have executed successfully");
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }
    }

    public class R2RBenchmark
    {
        [Benchmark]
        public void SimpleMethod()
        {
            // I don't believe there is a simple way to verify at runtime that we are actually running under r2r.
            // Reading PE format for app assemblies doesn't seem worth it.
        }
    }
}
