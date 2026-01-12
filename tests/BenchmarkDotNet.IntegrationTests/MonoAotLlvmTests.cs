using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
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
    /// Running these tests locally requires building the mono runtime from the dotnet/runtime repository,
    /// since the AOT compiler isn't distributed as a standalone package.
    ///
    /// To set up:
    /// 1. Clone https://github.com/dotnet/runtime
    /// 2. Build the mono runtime with libs:
    ///    Windows:  build.cmd -subset mono+libs -c Release
    ///    Unix:     ./build.sh -subset mono+libs -c Release
    /// 3. Set the MONOAOTLLVM_COMPILER_PATH environment variable to the compiler binary:
    ///    Windows:  artifacts\bin\mono\windows.x64.Release\mono-sgen.exe
    ///    Unix:     artifacts/bin/mono/[os].x64.Release/mono-sgen
    ///
    /// The runtime pack ends up at artifacts/bin/microsoft.netcore.app.runtime.[os]-x64/Release/
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
            // MonoAOTLLVM requires a 64-bit platform
            if (!RuntimeInformation.Is64BitPlatform())
                return false;

            if (!IsAotCompilerAvailable())
                return false;

            return true;
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
