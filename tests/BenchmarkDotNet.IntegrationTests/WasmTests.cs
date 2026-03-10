using System;
using System.IO;
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
using BenchmarkDotNet.Toolchains.MonoWasm;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    /// <summary>
    /// In order to run WasmTests locally, the following prerequisites are required:
    /// * Install wasm-tools workload: `dotnet install-wasm-tools-net8`
    /// * Install Node.js and add it to PATH
    /// * Install v8: `npm install jsvu -g && jsvu --os=default --engines=v8`
    /// * Add `$HOME/.jsvu/bin` to PATH
    /// </summary>
    public class WasmTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private const string V8SkipReason = "JSVU does not support ARM on Windows or Linux";

        [Theory]
        [InlineDataEnvSpecific([MonoAotCompilerMode.mini, "v8"], V8SkipReason, [EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm])]
        [InlineData(MonoAotCompilerMode.mini, "node")]
        // BUG: https://github.com/dotnet/BenchmarkDotNet/issues/3036
        [InlineData(MonoAotCompilerMode.wasm, "v8", Skip = "AOT is broken")]
        [InlineData(MonoAotCompilerMode.wasm, "node", Skip = "AOT is broken")]
        public void WasmIsSupported(MonoAotCompilerMode aotCompilerMode, string javaScriptEngine)
        {
            CanExecute<WasmBenchmark>(GetConfig(aotCompilerMode, javaScriptEngine));
        }

        [Theory]
        [InlineDataEnvSpecific([MonoAotCompilerMode.mini, "v8"], V8SkipReason, [EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm])]
        [InlineData(MonoAotCompilerMode.mini, "node")]
        // BUG: https://github.com/dotnet/BenchmarkDotNet/issues/3036
        [InlineData(MonoAotCompilerMode.wasm, "v8", Skip = "AOT is broken")]
        [InlineData(MonoAotCompilerMode.wasm, "node", Skip = "AOT is broken")]
        public void WasmSupportsInProcessDiagnosers(MonoAotCompilerMode aotCompilerMode, string javaScriptEngine)
        {
            try
            {
                var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
                var config = GetConfig(aotCompilerMode, javaScriptEngine).AddDiagnoser(diagnoser);

                CanExecute<WasmBenchmark>(config);

                Assert.Equal([diagnoser.ExpectedResult], diagnoser.Results.Values);
                Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result));
            }
            finally
            {
                BaseMockInProcessDiagnoser.s_completedResults.Clear();
            }
        }

        [Theory]
        [InlineDataEnvSpecific(["v8", "custom-main-v8.mjs", WasmIpcType.FileStdOut], V8SkipReason, [EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm])]
        [InlineData("node", "custom-main-node.mjs", WasmIpcType.WebSocket)]
        public void WasmSupportsCustomMainJs(string javaScriptEngine, string customMainJs, WasmIpcType ipcType)
        {
            var mainJsTemplate = new FileInfo(Path.Combine("wwwroot", customMainJs));
            var summary = CanExecute<WasmBenchmark>(GetConfig(MonoAotCompilerMode.mini, javaScriptEngine, mainJsTemplate: mainJsTemplate, ipcType: ipcType));

            var standardOutput = summary.Reports.Single().ExecuteResults.Single().StandardOutput;
            Assert.Contains($"hello from {customMainJs}", standardOutput);
        }

        private ManualConfig GetConfig(MonoAotCompilerMode aotCompilerMode, string javaScriptEngine, FileInfo? mainJsTemplate = null, WasmIpcType ipcType = WasmIpcType.Auto)
        {
            var dotnetVersion = "net8.0";
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: aotCompilerMode);

            return ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithRuntime(new WasmRuntime(dotnetVersion, RuntimeMoniker.WasmNet80, "wasm", aotCompilerMode == MonoAotCompilerMode.wasm, javaScriptEngine, mainJsTemplate: mainJsTemplate, ipcType: ipcType))
                    .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
                .WithBuildTimeout(TimeSpan.FromSeconds(240))
                .WithOption(ConfigOptions.LogBuildOutput, true)
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);
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
