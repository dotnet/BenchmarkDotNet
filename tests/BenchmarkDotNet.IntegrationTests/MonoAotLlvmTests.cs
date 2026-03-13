using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
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
    /// Running these tests requires building the Mono AOT runtime from dotnet/runtime.
    /// The toolchain isn't available as a standalone workload yet.
    ///
    /// Setup instructions:
    ///
    /// 1. Clone https://github.com/dotnet/runtime
    ///
    /// 2. Build the mono runtime with libraries:
    ///    Windows:  .\build.cmd -subset mono+libs -c Release
    ///    Linux:    ./build.sh -subset mono+libs -c Release
    ///    macOS:    ./build.sh -subset mono+libs -c Release
    ///
    /// 3. Set MONOAOTLLVM_COMPILER_PATH to the built compiler:
    ///    Windows:  set MONOAOTLLVM_COMPILER_PATH=C:\path\to\runtime\artifacts\bin\mono\windows.x64.Release\mono-sgen.exe
    ///    Unix:     export MONOAOTLLVM_COMPILER_PATH=/path/to/runtime/artifacts/bin/mono/linux.x64.Release/mono-sgen
    ///
    /// The runtime pack is at: artifacts/bin/microsoft.netcore.app.runtime.[os]-x64/Release/
    /// </summary>
    public class MonoAotLlvmTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private const string AotCompilerPathEnvVar = "MONOAOTLLVM_COMPILER_PATH";

        private static string GetAotCompilerPath()
            => Environment.GetEnvironmentVariable(AotCompilerPathEnvVar);

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

        [FactEnvSpecific("Requires Mono AOT LLVM toolchain from dotnet/runtime build", EnvRequirement.MonoAotLlvmToolchain)]
        public void MonoAotLlvmIsSupported()
        {
            CanExecute<MonoAotLlvmBenchmark>(GetConfig());
        }

        [FactEnvSpecific("Requires Mono AOT LLVM toolchain from dotnet/runtime build", EnvRequirement.MonoAotLlvmToolchain)]
        public void MonoAotLlvmSupportsInProcessDiagnosers()
        {
            try
            {
                var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
                var config = GetConfig().AddDiagnoser(diagnoser);

                CanExecute<MonoAotLlvmBenchmark>(config);

                Assert.Equal([diagnoser.ExpectedResult], diagnoser.Results.Values);
                Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result));
            }
            finally
            {
                BaseMockInProcessDiagnoser.s_completedResults.Clear();
            }
        }

        [FactEnvSpecific("Requires Mono AOT LLVM toolchain from dotnet/runtime build", EnvRequirement.MonoAotLlvmToolchain)]
        public void MonoAotLlvmMiniModeIsSupported()
        {
            CanExecute<MonoAotLlvmBenchmark>(GetConfig(MonoAotCompilerMode.mini));
        }

        public class MonoAotLlvmBenchmark
        {
            [Benchmark]
            public void Check()
            {
                if (!RuntimeInformation.IsMono)
                    throw new Exception("Expected Mono runtime");

                if (!RuntimeInformation.IsAot)
                    throw new Exception("Expected AOT mode");
            }
        }
    }
}
