using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Wasm;

namespace BenchmarkDotNet.Samples
{
    // *** Command Line Arguments ***
    public class IntroWasmCmdConfig
    {
        // Example:
        // --runtimes monowasm10.0
        // --cli /path/to/dotnet (optional)
        // --wasmEngine node (optional)
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
            var toolChain = CsProjMonoWasmToolchain.From(MonoWasmRuntime.Net10_0, WasmSettings.Default with { JavaScriptEngine = "node" });

            BenchmarkRunner.Run<IntroWasmFluentConfig>(DefaultConfig.Instance
                .AddJob(Job.ShortRun.WithToolchain(toolChain)));
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}
