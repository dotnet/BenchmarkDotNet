using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.NativeAot;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NativeAotTests : BenchmarkTestExecutor
    {
        public NativeAotTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [FactEnvSpecific("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET Core or NativeAOT process", EnvRequirement.DotNetCoreOnly)]
        public void LatestNativeAotVersionIsSupported()
        {
            if (!RuntimeInformation.Is64BitPlatform()) // NativeAOT does not support 32bit yet
                return;
            if (ContinuousIntegration.IsGitHubActionsOnWindows()) // no native dependencies installed
                return;
            if (RuntimeInformation.IsMacOS())
                return; // currently not supported

            var toolchain = NativeAotToolchain.CreateBuilder().UseNuGet().IlcInstructionSet(IsAvx2Supported() ? "avx2" : "").ToToolchain();

            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.GetCurrentVersion()) // we test against latest version for current TFM to make sure we avoid issues like #1055
                    .WithToolchain(toolchain)
                    .WithEnvironmentVariable(NativeAotBenchmark.EnvVarKey, IsAvx2Supported().ToString().ToLower()));

            try
            {
                CanExecute<NativeAotBenchmark>(config);
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }

        private bool IsAvx2Supported()
        {
#if NET6_0_OR_GREATER
            return System.Runtime.Intrinsics.X86.Avx2.IsSupported;
#else
            return false;
#endif
        }
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