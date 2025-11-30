using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.NativeAot;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NativeAotTests(ITestOutputHelper outputHelper) : BenchmarkTestExecutor(outputHelper)
    {
        private bool IsAvx2Supported()
        {
#if NET6_0_OR_GREATER
            return System.Runtime.Intrinsics.X86.Avx2.IsSupported;
#else
            return false;
#endif
        }

        private ManualConfig GetConfig()
        {
            var toolchain = NativeAotToolchain.CreateBuilder().UseNuGet().IlcInstructionSet(IsAvx2Supported() ? "avx2" : "").ToToolchain();

            return ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.GetCurrentVersion()) // we test against latest version for current TFM to make sure we avoid issues like #1055
                    .WithToolchain(toolchain)
                    .WithEnvironmentVariable(NativeAotBenchmark.EnvVarKey, IsAvx2Supported().ToString().ToLower()));
        }

        [FactEnvSpecific("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET Core or NativeAOT process", EnvRequirement.DotNetCoreOnly)]
        public void LatestNativeAotVersionIsSupported()
        {
            if (!GetShouldRunTest())
                return;

            try
            {
                CanExecute<NativeAotBenchmark>(GetConfig());
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }

        [FactEnvSpecific("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET Core or NativeAOT process", EnvRequirement.DotNetCoreOnly)]
        public void NativeAotSupportsInProcessDiagnosers()
        {
            if (!GetShouldRunTest())
                return;

            var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
            var config = GetConfig().AddDiagnoser(diagnoser);

            try
            {
                CanExecute<NativeAotBenchmark>(config);
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                {
                    Output.WriteLine(e.SkipMessage);
                    return;
                }
                throw;
            }

            Assert.Equal([diagnoser.ExpectedResult], diagnoser.Results.Values);
            Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults);
            BaseMockInProcessDiagnoser.s_completedResults.Clear();
        }

        private static bool GetShouldRunTest()
            => RuntimeInformation.Is64BitPlatform() // NativeAOT does not support 32bit yet
                && !ContinuousIntegration.IsGitHubActionsOnWindows() // no native dependencies installed
                && !OsDetector.IsMacOS(); // currently not supported
    }

    public class NativeAotBenchmark
    {
        internal const string EnvVarKey = "AVX2_IsSupported";

        [Benchmark]
        public void Check()
        {
            if (!RuntimeInformation.IsNativeAOT)
                throw new Exception("This is NOT NativeAOT");
#if NET6_0_OR_GREATER
            if (System.Runtime.Intrinsics.X86.Avx2.IsSupported != bool.Parse(Environment.GetEnvironmentVariable(EnvVarKey)))
                throw new Exception("Unexpected Instruction Set");
#endif
        }
    }
}