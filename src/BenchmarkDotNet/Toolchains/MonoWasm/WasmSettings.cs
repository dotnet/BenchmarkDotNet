using System;
namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmSettings
    {
        public string WasmMainJS { get; }

        public string WasmJavaScriptEngine { get; }

        public string WasmJavaScriptEngineArguments { get;  }

        public WasmSettings(string wasmMainJS, string wasmJavaScriptEngine, string wasmjavaScriptEngineArguments)
        {
            WasmMainJS = wasmMainJS;
            WasmJavaScriptEngine = wasmJavaScriptEngine;
            WasmJavaScriptEngineArguments = wasmjavaScriptEngineArguments;
        }
    }
}
