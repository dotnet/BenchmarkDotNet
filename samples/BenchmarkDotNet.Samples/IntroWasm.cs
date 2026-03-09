using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoWasm;

namespace BenchmarkDotNet.Samples
{
    // *** Command Line Arguments ***
    public class IntroWasmCmdConfig
    {
        // Example:
        // --runtimes wasmnet8.0
        // --cli /path/to/dotnet (optional)
        // --wasmEngine v8 (optional)
        // --wasmArgs "--expose_wasm" (optional)
        // --wasmDataDir /path/to/data (optional)
        public static void Run(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(IntroWasmCmdConfig).Assembly).Run(args);

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }

    // *** Fluent Config ***
    public class IntroWasmFluentConfig
    {
        public static void Run()
        {
            // Optional: set this to use a custom `dotnet` (for example, a local dotnet/runtime build).
            const string cliPath = "";

            WasmRuntime runtime = new WasmRuntime(msBuildMoniker: "net8.0", RuntimeMoniker.WasmNet80, "Wasm .net8.0", false, "v8");
            NetCoreAppSettings netCoreAppSettings = new NetCoreAppSettings(
                targetFrameworkMoniker: "net8.0", runtimeFrameworkVersion: "", name: "Wasm",
                customDotNetCliPath: cliPath);
            var toolChain = WasmToolchain.From(netCoreAppSettings);

            BenchmarkRunner.Run<IntroWasmFluentConfig>(DefaultConfig.Instance
                .AddJob(Job.ShortRun.WithRuntime(runtime).WithToolchain(toolChain)));
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}
