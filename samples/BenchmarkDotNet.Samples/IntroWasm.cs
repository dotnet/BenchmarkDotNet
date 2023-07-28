using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.MonoWasm;

namespace BenchmarkDotNet.Samples
{
    // *** Command Line Arguments ***
    public class IntroWasmCmdConfig
    {
        // the args must contain:
        // an information that we want to run benchmark as Wasm:
        // --runtimes Wasm
        // path to dotnet cli
        // --cli /home/adam/projects/runtime/dotnet.sh
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
            // the Wasm Toolchain requires two mandatory arguments:
            const string cliPath = @"/home/adam/projects/runtime/dotnet.sh";

            WasmRuntime runtime = new WasmRuntime(msBuildMoniker: "net5.0");
            NetCoreAppSettings netCoreAppSettings = new NetCoreAppSettings(
                targetFrameworkMoniker: "net5.0", runtimeFrameworkVersion: null, name: "Wasm",
                customDotNetCliPath: cliPath);
            IToolchain toolChain = WasmToolchain.From(netCoreAppSettings);

            BenchmarkRunner.Run<IntroCustomMonoFluentConfig>(DefaultConfig.Instance
                .AddJob(Job.ShortRun.WithRuntime(runtime).WithToolchain(toolChain)));
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}