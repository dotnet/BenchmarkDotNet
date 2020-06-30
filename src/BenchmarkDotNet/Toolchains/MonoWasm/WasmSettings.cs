using System;
namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmSettings
    {
        public string WasmMainJS { get; }

        public string WasmRuntimePack { get; }

        public string WasmAppBuilder { get; }

        public string WasmJavaScriptEngine { get; }


        public WasmSettings(string wasmMainJS, string wasmRuntimePack, string wasmAppBuilder, string wasmJavaScriptEngine)
        {
            WasmMainJS = wasmMainJS;
            WasmRuntimePack = wasmRuntimePack;
            WasmAppBuilder = wasmAppBuilder;
            WasmJavaScriptEngine = wasmJavaScriptEngine;
        }
    }
}
