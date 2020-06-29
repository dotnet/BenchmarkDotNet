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
        [PublicAPI] public static readonly IToolchain NetCoreApp50Wasm = From(NetCoreAppSettings.NetCoreApp50, new WasmSettings(null, null, null));

        private WasmToolChain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string runtimeJavaScriptPath)
            : base(name, generator, builder, executor)
        {
        }

        public WasmToolChain(string name,
                             string targetFrameworkMoniker,
                             string cliPath,
                             string packagesPath,
                             string runtimePackPath,
                             string wasmAppBuilderAssembly,
                             string mainJS,
                             TimeSpan timeout)
           : base(name,
                 new WasmGenerator(targetFrameworkMoniker,
                                   cliPath,
                                   packagesPath,
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
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings,  WasmSettings wasmSettings)
        {

            return new  WasmToolChain(netCoreAppSettings.Name,
                                      new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                                                        netCoreAppSettings.CustomDotNetCliPath,
                                                        netCoreAppSettings.PackagesPath,
                                                        wasmSettings.WasmRuntimePack,
                                                        wasmSettings.WasmAppBuilder,
                                                        wasmSettings.WasmMainJS),
                                      new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                                                           netCoreAppSettings.CustomDotNetCliPath,
                                                           netCoreAppSettings.Timeout),
                                      new WasmExecutor(wasmSettings.WasmMainJS),
                netCoreAppSettings.CustomDotNetCliPath);
        }

    }
}