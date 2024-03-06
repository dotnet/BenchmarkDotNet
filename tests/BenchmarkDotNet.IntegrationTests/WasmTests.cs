using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoWasm;
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
        [FactEnvSpecific("WASM is only supported on Unix", EnvRequirement.NonWindows)]
        public void WasmIsSupported()
        {
            var dotnetVersion = "net8.0";
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, null, "Wasm");
            var mainJsPath = Path.Combine(AppContext.BaseDirectory, "AppBundle", "test-main.js");

            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildArgument($"/p:WasmMainJSPath={mainJsPath}")])
                    .WithRuntime(new WasmRuntime(dotnetVersion, moniker: RuntimeMoniker.WasmNet80, javaScriptEngineArguments: "--expose_wasm --module"))
                    .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, true);

            CanExecute<WasmBenchmark>(config);
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
