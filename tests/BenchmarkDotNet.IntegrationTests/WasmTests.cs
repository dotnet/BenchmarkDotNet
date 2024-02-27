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
    public class WasmTests : BenchmarkTestExecutor
    {
        public WasmTests(ITestOutputHelper output) : base(output) { }

        [FactEnvSpecific("WASM is only supported on Unix", EnvRequirement.NonWindows)]
        public void WasmIsSupported()
        {
            var dotnetVersion = "net8.0";
            var logger = new OutputLogger(Output);
            var netCoreAppSettings = new NetCoreAppSettings(dotnetVersion, null, "Wasm");
            var mainJsPath = Path.Combine(AppContext.BaseDirectory, "AppBundle", "test-main.js");

            // Since WasmExecutor uses ShellExecute=false, it is not searching the path and needs a full path
            // Maybe ShellExecute should be true? Otherwise not sure of a good way to get the v8 path in the integration test.
            var v8Path = "/home/caaavik/.jsvu/bin/v8";
            var config = ManualConfig.CreateEmpty()
                .AddLogger(logger)
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildArgument($"/p:WasmMainJSPath={mainJsPath}")])
                    .WithRuntime(new WasmRuntime(dotnetVersion, javaScriptEngine: v8Path, moniker: RuntimeMoniker.WasmNet70, javaScriptEngineArguments: "--expose_wasm --module"))
                    .WithToolchain(WasmToolchain.From(netCoreAppSettings)))
                .WithOption(ConfigOptions.GenerateMSBuildBinLog, true);

            CanExecute<WasmBenchmark>(config);
        }

        public class WasmBenchmark
        {
            [Benchmark]
            public void Check()
            {
                // See if there are any other checks to run to make sure the benchmark is running in a WASM environment.

                if (RuntimeInformation.GetCurrentRuntime().RuntimeMoniker != RuntimeMoniker.WasmNet70)
                {
                    throw new Exception("Incorrect runtime detection");
                }
            }
        }
    }
}
