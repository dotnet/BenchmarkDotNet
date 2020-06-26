using System;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;


namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    [PublicAPI]
    public class WasmAppSettings : NetCoreAppSettings
    {
        [PublicAPI] public static readonly WasmAppSettings NetCoreApp50Wasm = new WasmAppSettings("net5.0", null, ".NET Core 5.0 wasm", new WasmSettings(null, null, null), null);

        public WasmSettings WasmSettings { get; }

        public WasmAppSettings(string targetFrameworkMoniker,
                               string runtimeFrameworkVersion,
                               string name,
                               WasmSettings wasmSettings,
                               string customDotNetCliPath = null,
                               string packagesPath = null,
                               TimeSpan? timeout = null)
         : base(targetFrameworkMoniker,
               runtimeFrameworkVersion,
               name,
               customDotNetCliPath,
               packagesPath,
               timeout)
        {
            WasmSettings = wasmSettings;
        }


        public override NetCoreAppSettings WithCustomDotNetCliPath(string customDotNetCliPath, string displayName = null)
            => new WasmAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, displayName ?? Name,  WasmSettings, customDotNetCliPath, PackagesPath, Timeout);

        public override NetCoreAppSettings WithCustomPackagesRestorePath(string packagesPath, string displayName = null)
            => new WasmAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, displayName ?? Name, WasmSettings,CustomDotNetCliPath, packagesPath, Timeout);

        public override NetCoreAppSettings WithTimeout(TimeSpan? timeOut)
            => new WasmAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, Name, WasmSettings, CustomDotNetCliPath, PackagesPath, timeOut ?? Timeout);

        public WasmAppSettings WithWasmMainSettings(WasmSettings newWasmSettings)
             => new WasmAppSettings(TargetFrameworkMoniker, RuntimeFrameworkVersion, Name, newWasmSettings, CustomDotNetCliPath, PackagesPath, Timeout);

    }



}
