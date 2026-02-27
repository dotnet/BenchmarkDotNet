using System;
using System.ComponentModel;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class WasmRuntime : Runtime, IEquatable<WasmRuntime>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly WasmRuntime Default = new WasmRuntime();

        public string JavaScriptEngine { get; }

        public string JavaScriptEngineArguments { get; }

        public bool Aot { get; }

        public string WasmDataDir { get; }

        /// <summary>
        /// Specifies the runtime flavor used for WASM benchmarks. <see cref="Environments.RuntimeFlavor.Mono"/> (default) resolves the
        /// Mono runtime pack (Microsoft.NETCore.App.Runtime.Mono.browser-wasm); <see cref="Environments.RuntimeFlavor.CoreCLR"/> resolves
        /// the CoreCLR runtime pack (Microsoft.NETCore.App.Runtime.browser-wasm).
        /// </summary>
        public RuntimeFlavor RuntimeFlavor { get; }

        /// <summary>
        /// Maximum time in minutes to wait for a single benchmark process to finish before force killing it. Default is 10 minutes.
        /// </summary>
        public int ProcessTimeoutMinutes { get; }

        /// <summary>
        /// creates new instance of WasmRuntime
        /// </summary>
        /// <param name="javaScriptEngine">Full path to a java script engine used to run the benchmarks. "v8" by default</param>
        /// <param name="javaScriptEngineArguments">Arguments for the javascript engine. "--expose_wasm" by default</param>
        /// <param name="msBuildMoniker">moniker, default: "net5.0"</param>
        /// <param name="displayName">default: "Wasm"</param>
        /// <param name="aot">Specifies whether AOT or Interpreter (default) project should be generated.</param>
        /// <param name="wasmDataDir">Specifies a wasm data directory surfaced as $(WasmDataDir) for the project</param>
        /// <param name="moniker">Runtime moniker</param>
        /// <param name="runtimeFlavor">Runtime flavor to use: Mono (default) or CoreCLR.</param>
        /// <param name="processTimeoutMinutes">Maximum time in minutes to wait for a single benchmark process to finish. Default is 10.</param>
        public WasmRuntime(
            string msBuildMoniker = "net8.0",
            string displayName = "Wasm",
            string javaScriptEngine = "v8",
            string javaScriptEngineArguments = "--expose_wasm",
            bool aot = false,
            string wasmDataDir = "",
            RuntimeMoniker moniker = RuntimeMoniker.WasmNet80,
            RuntimeFlavor runtimeFlavor = RuntimeFlavor.Mono,
            int processTimeoutMinutes = 10)
            : base(moniker, msBuildMoniker, displayName)
        {
            if (javaScriptEngine.IsNotBlank() && javaScriptEngine != "v8" && !File.Exists(javaScriptEngine))
                throw new FileNotFoundException($"Provided {nameof(javaScriptEngine)} file: \"{javaScriptEngine}\" doest NOT exist");

            JavaScriptEngine = javaScriptEngine;
            JavaScriptEngineArguments = javaScriptEngineArguments;
            Aot = aot;
            WasmDataDir = wasmDataDir;
            RuntimeFlavor = runtimeFlavor;
            ProcessTimeoutMinutes = processTimeoutMinutes;
        }

        public override bool Equals(object? obj)
            => obj is WasmRuntime other && Equals(other);

        public bool Equals(WasmRuntime? other)
            => other != null && base.Equals(other) && other.JavaScriptEngine == JavaScriptEngine && other.JavaScriptEngineArguments == JavaScriptEngineArguments && other.Aot == Aot && other.RuntimeFlavor == RuntimeFlavor;

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), JavaScriptEngine, JavaScriptEngineArguments, Aot, RuntimeFlavor);
    }
}