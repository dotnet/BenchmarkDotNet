using System;
namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmSettings
    {
        public string WasmMainJS { get; }

        public string WasmRuntimePack { get; }

        public string WasmAppBuilder { get; }

        public WasmSettings(string wasmMainJS, string wasmRuntimePack, string wasmAppBuilder)
        {
            WasmMainJS = wasmMainJS;
            WasmRuntimePack = wasmRuntimePack;
            WasmAppBuilder = wasmAppBuilder;
        }
    }
}
