using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    /// <summary>
    /// In order to run WasmTests locally, the following prerequisites are required:
    /// * Install wasm-tools workload: `BenchmarkDotNet/build.cmd install-wasm-tools`
    /// * Install npm
    /// * Install v8: `npm install jsvu -g && jsvu --os=default --engines=v8`
    /// * Add `$HOME/.jsvu/bin` to PATH
    /// * Run tests using .NET SDK from `BenchmarkDotNet/.dotnet/`
    /// </summary>
    public class WasmTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private ManualConfig GetConfig(MonoAotCompilerMode aotCompilerMode)
        {
            var dotnetVersion = "net8.0";
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, null, "Wasm", aotCompilerMode: aotCompilerMode);
            const string mainJsName = "benchmark-main.mjs";
            var mainJsDir = Path.Combine(Path.GetTempPath(), "BenchmarkDotNet.IntegrationTests");
            Directory.CreateDirectory(mainJsDir);
            var mainJsPath = Path.Combine(mainJsDir, mainJsName);
            File.WriteAllText(mainJsPath, ResourceHelper.LoadTemplate(mainJsName));

            return ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildArgument($"/p:WasmMainJSPath={mainJsPath}")])
                    .WithRuntime(new WasmRuntime(dotnetVersion, moniker: RuntimeMoniker.WasmNet80, javaScriptEngineArguments: "--expose_wasm --module"))
                    .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
                .WithBuildTimeout(TimeSpan.FromSeconds(240))
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, true);
        }

        [TheoryEnvSpecific("WASM is only supported on Unix", EnvRequirement.NonWindows)]
        [InlineData(MonoAotCompilerMode.mini)]
        [InlineData(MonoAotCompilerMode.wasm)]
        public void WasmIsSupported(MonoAotCompilerMode aotCompilerMode)
        {
            // Test fails on Linux non-x64.
            if (OsDetector.IsLinux() && RuntimeInformation.GetCurrentPlatform() != Platform.X64)
            {
                return;
            }

            CanExecute<WasmBenchmark>(GetConfig(aotCompilerMode));
        }

        [TheoryEnvSpecific("WASM is only supported on Unix", EnvRequirement.NonWindows)]
        [InlineData(MonoAotCompilerMode.mini)]
        [InlineData(MonoAotCompilerMode.wasm)]
        public void WasmSupportsInProcessDiagnosers(MonoAotCompilerMode aotCompilerMode)
        {
            // Test fails on Linux non-x64.
            if (OsDetector.IsLinux() && RuntimeInformation.GetCurrentPlatform() != Platform.X64)
            {
                return;
            }

            try
            {
                var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
                var config = GetConfig(aotCompilerMode).AddDiagnoser(diagnoser);

                CanExecute<WasmBenchmark>(config);

                Assert.Equal([diagnoser.ExpectedResult], diagnoser.Results.Values);
                Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result));
            }
            finally
            {
                BaseMockInProcessDiagnoser.s_completedResults.Clear();
            }
        }

        public class WasmBenchmark
        {
            [Benchmark]
            public void Check()
            {
                if (!RuntimeInformation.IsWasm)
                {
                    throw new Exception("Incorrect runtime detection");
                }
            }
        }
    }
}
