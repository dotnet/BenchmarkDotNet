using System;
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
        [PublicAPI] public static readonly IToolchain NetCoreApp50Wasm = From(WasmAppSettings.NetCoreApp50Wasm);

        private WasmToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string runtimeJavaScriptPath)
            : base(name, generator, builder, executor)
        {
        }

        public WasmToolChain(string name,
                             string targetFrameworkMoniker,
                             string cliPath,
                             string packagesPath,
                             string runtimeFrameworkVersion,
                             string runtimePackPath,
                             string wasmAppBuilderAssembly,
                             string mainJS,
                             TimeSpan timeout)
           : base(name,
                 new WasmGenerator(targetFrameworkMoniker,
                                   cliPath,
                                   packagesPath,
                                   runtimeFrameworkVersion,
                                   runtimePackPath,
                                   wasmAppBuilderAssembly,
                                   mainJS),
                 new DotNetCliBuilder(targetFrameworkMoniker, cliPath, timeout),
                 new WasmExecutor(mainJS)
                 )
        {
        }


        internal string RuntimeJavaScripthPath { get; }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            return !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings settings)
        {
            WasmAppSettings wasmAppSettings = (WasmAppSettings)settings;

            return new  WasmToolChain(settings.Name,
                                      new WasmGenerator(wasmAppSettings.TargetFrameworkMoniker,
                                                        wasmAppSettings.CustomDotNetCliPath,
                                                        wasmAppSettings.PackagesPath,
                                                        wasmAppSettings.RuntimeFrameworkVersion,
                                                        wasmAppSettings.WasmSettings.WasmRuntimePack,
                                                        wasmAppSettings.WasmSettings.WasmAppBuilder,
                                                        wasmAppSettings.WasmSettings.WasmMainJS),
                                      new DotNetCliBuilder(settings.TargetFrameworkMoniker,
                                                           settings.CustomDotNetCliPath,
                                                           settings.Timeout),
                                      new WasmExecutor(wasmAppSettings.WasmSettings.WasmMainJS),
                settings.CustomDotNetCliPath);
        }

    }
}