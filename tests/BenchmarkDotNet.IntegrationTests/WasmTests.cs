using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
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
    /// * Install wasm-tools workload: `BenchmarkDotNet/build.cmd install-wasm-tools`
    /// * Install npm
    /// * Install v8: `npm install jsvu -g && jsvu --os=default --engines=v8`
    /// * Add `$HOME/.jsvu/bin` to PATH
    /// * Run tests using .NET SDK from `BenchmarkDotNet/.dotnet/`
    /// </summary>
    public class WasmTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        [Theory]
        [InlineData(MonoAotCompilerMode.mini)]
        [InlineData(MonoAotCompilerMode.wasm)]
        public void WasmIsSupported(MonoAotCompilerMode aotCompilerMode)
        {
            if (SkipTestRun(aotCompilerMode))
            {
                return;
            }

            CanExecute<WasmBenchmark>(GetConfig(aotCompilerMode));
        }

        [Theory]
        [InlineData(MonoAotCompilerMode.mini)]
        [InlineData(MonoAotCompilerMode.wasm)]
        public void WasmSupportsInProcessDiagnosers(MonoAotCompilerMode aotCompilerMode)
        {
            if (SkipTestRun(aotCompilerMode))
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

        [Fact]
        public void WasmSupportsCustomMainJs()
        {
            if (SkipTestRun())
            {
                return;
            }

            var summary = CanExecute<WasmBenchmark>(GetConfig(MonoAotCompilerMode.mini, true, true));

            var artefactsPaths = summary.Reports.Single().GenerateResult.ArtifactsPaths;
            Assert.Contains("custom-template-identifier", File.ReadAllText(artefactsPaths.ExecutablePath));

            Directory.Delete(Path.GetDirectoryName(artefactsPaths.ProjectFilePath)!, true);
        }

        private ManualConfig GetConfig(MonoAotCompilerMode aotCompilerMode, bool useMainJsTemplate = false, bool keepBenchmarkFiles = false)
        {
            var dotnetVersion = "net8.0";
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: aotCompilerMode);

            var mainJsTemplatePath = useMainJsTemplate ? Path.Combine("wwwroot", "custom-main.mjs") : null;

            return ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithRuntime(new WasmRuntime(dotnetVersion, RuntimeMoniker.WasmNet80, "wasm", aotCompilerMode == MonoAotCompilerMode.wasm, "v8"))
                    .WithToolchain(WasmToolchain.From(netCoreAppSettings, mainJsTemplatePath)))
                .WithBuildTimeout(TimeSpan.FromSeconds(240))
                .WithOption(ConfigOptions.KeepBenchmarkFiles, keepBenchmarkFiles)
                .WithOption(ConfigOptions.LogBuildOutput, true)
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, false);
        }

        private static bool SkipTestRun(MonoAotCompilerMode aotCompilerMode = MonoAotCompilerMode.mini)
        {
            // jsvu only supports arm for mac.
            if (RuntimeInformation.GetCurrentPlatform() != Platform.X64 && !OsDetector.IsMacOS())
            {
                return true;
            }

            // AOT crashes because of BenchmarkDotNet.Running.WakeLock+PInvoke.
            if (OsDetector.IsWindows() && aotCompilerMode == MonoAotCompilerMode.wasm)
                return true;

            return false;
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
