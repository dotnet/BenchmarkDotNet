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
                             WasmSettings wasmSettings,
                             TimeSpan timeout)
           : base(name,
                 new WasmGenerator(targetFrameworkMoniker,
                                   cliPath,
                                   packagesPath,
                                   wasmSettings.WasmMainJS),
                 new WasmBuilder(targetFrameworkMoniker, wasmSettings, cliPath, timeout),
                 new WasmExecutor(wasmSettings.WasmMainJS, wasmSettings.WasmJavaScriptEngine,  wasmSettings.WasmJavaScriptEngineArguments))
        {
        }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
                return false;

            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.WriteLineError($"{nameof(WasmToolChain)} is supported only on Unix, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            return true;
        }

        [PublicAPI]
        public static IToolchain From(NetCoreAppSettings netCoreAppSettings,  WasmSettings wasmSettings)
        {
            return new  WasmToolChain(netCoreAppSettings.Name,
                                      new WasmGenerator(netCoreAppSettings.TargetFrameworkMoniker,
                                                        netCoreAppSettings.CustomDotNetCliPath,
                                                        netCoreAppSettings.PackagesPath,
                                                        wasmSettings.WasmMainJS),
                                      new DotNetCliBuilder(netCoreAppSettings.TargetFrameworkMoniker,
                                                           netCoreAppSettings.CustomDotNetCliPath,
                                                           netCoreAppSettings.Timeout),
                                      new WasmExecutor(wasmSettings.WasmMainJS, wasmSettings.WasmJavaScriptEngine, wasmSettings.WasmJavaScriptEngineArguments),
                netCoreAppSettings.CustomDotNetCliPath);
        }
    }
}
