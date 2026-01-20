using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.R2R;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class R2RTests : BenchmarkTestExecutor
    {
        public R2RTests(ITestOutputHelper output) : base(output) { }

        private ManualConfig GetConfig()
        {
            var toolchain = R2RToolchain.From(
                new NetCoreAppSettings(
                    targetFrameworkMoniker: GetTargetFrameworkMoniker(),
                    runtimeFrameworkVersion: null,
                    name: "R2RTest"));

            return ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(GetCurrentR2RRuntime())
                    .WithToolchain(toolchain));
        }

        private static string GetTargetFrameworkMoniker()
        {
#if NET10_0_OR_GREATER
            return "net10.0";
#elif NET9_0_OR_GREATER
            return "net9.0";
#elif NET8_0_OR_GREATER
            return "net8.0";
#else
            throw new NotSupportedException("R2R tests require .NET 8.0 or later");
#endif
        }

        private static R2RRuntime GetCurrentR2RRuntime()
        {
#if NET10_0_OR_GREATER
            return R2RRuntime.Net10_0;
#elif NET9_0_OR_GREATER
            return R2RRuntime.Net90;
#elif NET8_0_OR_GREATER
            return R2RRuntime.Net80;
#else
            throw new NotSupportedException("R2R tests require .NET 8.0 or later");
#endif
        }

        [FactEnvSpecific("R2R requires .NET Core runtime", EnvRequirement.DotNetCoreOnly)]
        public void R2RToolchainCanExecuteBenchmarks()
        {
            try
            {
                var summary = CanExecute<R2RBenchmark>(GetConfig());

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
            // I don't believe there is a way to verify at runtime that we are actrually running under r2r
        }
    }
}