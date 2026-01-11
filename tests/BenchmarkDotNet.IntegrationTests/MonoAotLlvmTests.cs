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
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    /// <summary>
    /// In order to run MonoAotLlvmTests locally, the following prerequisites are required:
    /// * Have MonoAOT workload installed
    /// * Have the Mono AOT compiler available at the path specified by MONOAOTLLVM_COMPILER_PATH environment variable
    /// </summary>
    public class MonoAotLlvmTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private const string AotCompilerPathEnvVar = "MONOAOTLLVM_COMPILER_PATH";

        private static string GetAotCompilerPath()
            => Environment.GetEnvironmentVariable(AotCompilerPathEnvVar);

        private static bool IsAotCompilerAvailable()
            => !string.IsNullOrEmpty(GetAotCompilerPath()) && System.IO.File.Exists(GetAotCompilerPath());

        private ManualConfig GetConfig(MonoAotCompilerMode mode = MonoAotCompilerMode.llvm)
        {
            var aotCompilerPath = GetAotCompilerPath();
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(
                "net8.0",
                null,
                "MonoAOTLLVM",
                aotCompilerPath: aotCompilerPath,
                aotCompilerMode: mode);

            return ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithRuntime(new MonoAotLLVMRuntime(
                        new System.IO.FileInfo(aotCompilerPath),
                        mode,
                        "net8.0"))
                    .WithToolchain(MonoAotLLVMToolChain.From(netCoreAppSettings)))
                .WithBuildTimeout(TimeSpan.FromMinutes(5))
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, true);
        }

        private static bool GetShouldRunTest()
        {
            // MonoAOTLLVM is only supported on non-Windows platforms with a 64-bit architecture
            if (!RuntimeInformation.Is64BitPlatform())
                return false;

            if (OsDetector.IsWindows())
                return false;

            if (!IsAotCompilerAvailable())
                return false;

            return true;
        }

        [FactEnvSpecific("MonoAOTLLVM is only supported on Unix", EnvRequirement.NonWindows)]
        public void MonoAotLlvmIsSupported()
        {
            if (!GetShouldRunTest())
            {
                Output.WriteLine($"Skipping test: AOT compiler not available at {AotCompilerPathEnvVar}");
                return;
            }

            try
            {
                CanExecute<MonoAotLlvmBenchmark>(GetConfig());
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }

        [FactEnvSpecific("MonoAOTLLVM is only supported on Unix", EnvRequirement.NonWindows)]
        public void MonoAotLlvmSupportsInProcessDiagnosers()
        {
            if (!GetShouldRunTest())
            {
                Output.WriteLine($"Skipping test: AOT compiler not available at {AotCompilerPathEnvVar}");
                return;
            }

            try
            {
                var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
                var config = GetConfig().AddDiagnoser(diagnoser);

                try
                {
                    CanExecute<MonoAotLlvmBenchmark>(config);
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
                Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result));
            }
            finally
            {
                BaseMockInProcessDiagnoser.s_completedResults.Clear();
            }
        }

        [FactEnvSpecific("MonoAOTLLVM is only supported on Unix", EnvRequirement.NonWindows)]
        public void MonoAotLlvmMiniModeIsSupported()
        {
            if (!GetShouldRunTest())
            {
                Output.WriteLine($"Skipping test: AOT compiler not available at {AotCompilerPathEnvVar}");
                return;
            }

            try
            {
                CanExecute<MonoAotLlvmBenchmark>(GetConfig(MonoAotCompilerMode.mini));
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                    Output.WriteLine(e.SkipMessage);
                else
                    throw;
            }
        }

        public class MonoAotLlvmBenchmark
        {
            [Benchmark]
            public void Check()
            {
                // Verify we're running on Mono AOT
                if (!RuntimeInformation.IsMono)
                {
                    throw new Exception("This is not Mono runtime");
                }

                if (!RuntimeInformation.IsAot)
                {
                    throw new Exception("This is not running in AOT mode");
                }
            }
        }
    }
}
