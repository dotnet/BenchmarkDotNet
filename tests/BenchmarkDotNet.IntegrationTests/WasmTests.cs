using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.IntegrationTests.WasmBenchmarks;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Wasm;

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
        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(TestData.WasmIsSupported), MemberType = typeof(TestData))]

        public void WasmIsSupported(string runtimeMoniker, string javaScriptEngine)
        {
            CanExecute<WasmBenchmark>(GetConfig(runtimeMoniker, javaScriptEngine));
        }

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(TestData.WasmSupportsInProcessDiagnosers), MemberType = typeof(TestData))]
        public void WasmSupportsInProcessDiagnosers(string javaScriptEngine)
        {
            try
            {
                var diagnoser = new MockInProcessDiagnoser1(BenchmarkDotNet.Diagnosers.RunMode.NoOverhead);
                var config = GetConfig(RuntimeMoniker.MonoWasm10_0, javaScriptEngine).AddDiagnoser(diagnoser);

                CanExecute<WasmBenchmark>(config);

                Assert.Equal([diagnoser.ExpectedResult], diagnoser.Results.Values);
                Assert.Equal([diagnoser.ExpectedResult], BaseMockInProcessDiagnoser.s_completedResults.Select(t => t.result));
            }
            finally
            {
                BaseMockInProcessDiagnoser.s_completedResults.Clear();
            }
        }

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(TestData.WasmSupportsCustomMainJs), MemberType = typeof(TestData))]
        public void WasmSupportsCustomMainJs(string javaScriptEngine, string customMainJs, WasmIpcType ipcType)
        {
            var mainJsTemplate = new FileInfo(Path.Combine("wwwroot", customMainJs));
            var summary = CanExecute<WasmBenchmark>(GetConfig(RuntimeMoniker.MonoWasm10_0, javaScriptEngine, mainJsTemplate: mainJsTemplate, ipcType: ipcType));

            var standardOutput = summary.Reports.Single().ExecuteResults.Single().StandardOutput;
            Assert.Contains($"hello from {customMainJs}", standardOutput);
        }

        private ManualConfig GetConfig(string runtimeMoniker, string javaScriptEngine, FileInfo? mainJsTemplate = null, WasmIpcType ipcType = WasmIpcType.Auto)
        {
            var logger = new OutputLogger(Output);
            var wasmSettings = new WasmSettings { JavaScriptEngine = javaScriptEngine, MainJsTemplate = mainJsTemplate, IpcType = ipcType };
            IToolchain toolchain = Runtime.Parse(runtimeMoniker) switch
            {
                MonoWasmAotRuntime aot => CsProjMonoWasmAotToolchain.From(aot, wasmSettings),
                MonoWasmRuntime mono => CsProjMonoWasmToolchain.From(mono, wasmSettings),
                CoreWasmRuntime core => CsProjCoreWasmToolchain.From(core, wasmSettings),
                var other => throw new ArgumentException($"'{runtimeMoniker}' is not a WASM runtime (parsed as {other.GetType().Name}).", nameof(runtimeMoniker)),
            };

            return ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithToolchain(toolchain))
                .WithBuildTimeout(TimeSpan.FromSeconds(480)) // Increase timeout for `WasmSupportsInProcessDiagnosers` test on macos(x64)
                .WithOption(ConfigOptions.LogBuildOutput, true)
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);
        }
    }

    public static class TestData
    {
        public static IEnumerable<TheoryDataRow<string, string>> WasmIsSupported()
        {
            if (EnvRequirementChecker.GetSkip(EnvRequirement.NonGitHubDraftPR) != null)
                yield break;

            yield return new(RuntimeMoniker.MonoWasm10_0, "node");

            if (IsV8Supported())
            {
                yield return new(RuntimeMoniker.MonoWasm10_0, "v8");
                yield return new(RuntimeMoniker.MonoWasmAot10_0, "v8");

                // CoreWasm is not tested yet because it is still experimental.
                // yield return new(RuntimeMoniker.CoreWasm11_0, "v8");
            }

            // WASM AOT does not build on Windows arm64: the mono-aot-cross toolchain runs but does not emit the
            // *_compiled_methods.txt token file, so WasmApp.Common.targets fails. It works on Linux arm64.
            if (EnvRequirementChecker.GetSkip(EnvRequirement.NonWindowsArm) == null)
                yield return new(RuntimeMoniker.MonoWasmAot10_0, "node");

            // CoreWasm is not tested yet because it is still experimental.
            // yield return new(RuntimeMoniker.CoreWasm11_0, "node");
        }

        public static IEnumerable<TheoryDataRow<string>> WasmSupportsInProcessDiagnosers()
        {
            if (EnvRequirementChecker.GetSkip(EnvRequirement.NonGitHubDraftPR) != null)
                yield break;

            if (IsV8Supported())
                yield return new("v8");

            yield return new("node");
        }

        public static IEnumerable<TheoryDataRow<string, string, WasmIpcType>> WasmSupportsCustomMainJs()
        {
            if (EnvRequirementChecker.GetSkip(EnvRequirement.NonGitHubDraftPR) != null)
                yield break;

            if (IsV8Supported())
                yield return new("v8", "custom-main-v8.mjs", WasmIpcType.FileStdOut);

            yield return new("node", "custom-main-node.mjs", WasmIpcType.WebSocket);
        }

        // JSVU does not support ARM on Windows or Linux
        private static bool IsV8Supported()
            => EnvRequirementChecker.GetSkip([EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm]) == null;
    }
}
