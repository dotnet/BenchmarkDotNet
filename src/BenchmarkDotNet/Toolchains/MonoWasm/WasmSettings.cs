using System;
namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmSettings
    {
        public string WasmMainJS { get; }

        public string WasmRuntimePack { get; }

        public string WasmJavaScriptEngine { get; }

        public string WasmJavaScriptEngineArguments { get;  }


        public WasmSettings(string wasmMainJS, string wasmRuntimePack, string wasmJavaScriptEngine, string wasmjavaScriptEngineArguments)
        {
            WasmMainJS = wasmMainJS;
            WasmRuntimePack = wasmRuntimePack;
            WasmJavaScriptEngine = wasmJavaScriptEngine;
            WasmJavaScriptEngineArguments = wasmjavaScriptEngineArguments;
        }
    }
}
