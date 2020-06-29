using System.Runtime.InteropServices;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Loggers;




namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmToolChain : Toolchain
    {
        [PublicAPI] public static readonly IToolchain NetCoreApp50Wasm = From( NetCoreAppSettings.NetCoreApp50Wasm.WithCustomDotNetCliPath("/Users/naricc/workspace/runtime-webassembly-ci/dotnet.sh") );

        private WasmToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string runtimeJavaScriptPath)
            : base(name, generator, builder, executor)
        {
            RuntimeJavaScripthPath = runtimeJavaScriptPath;
        }

        internal string RuntimeJavaScripthPath { get; }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            return !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

         [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
            => new WasmToolChain(settings.Name,
                new WasmGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                new DotNetCliBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.Timeout),
                new WasmExecutor(settings.RuntimeJavaScriptPath),
                settings.CustomDotNetCliPath);


    }
}