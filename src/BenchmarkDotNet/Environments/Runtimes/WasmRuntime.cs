using System;
using System.ComponentModel;
using System.IO;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Environments
{
    public class WasmRuntime : Runtime, IEquatable<WasmRuntime>
    {
        public delegate string ArgumentFormatter(WasmRuntime runtime, ArtifactsPaths artifactsPaths, string args);

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly WasmRuntime Default = new WasmRuntime();

        public string JavaScriptEngine { get; }

        public string JavaScriptEngineArguments { get; }

        public ArgumentFormatter JavaScriptEngineArgumentFormatter { get; }

        public override bool IsAOT { get; }

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
        /// <param name="msBuildMoniker">moniker</param>
        /// <param name="moniker">Runtime moniker</param>
        /// <param name="displayName">display name</param>
        /// <param name="aot">Specifies whether AOT or Interpreter project should be generated.</param>
        /// <param name="javaScriptEngine">Full path to a java script engine used to run the benchmarks.</param>
        /// <param name="javaScriptEngineArguments">Arguments for the javascript engine.</param>
        /// <param name="runtimeFlavor">Runtime flavor to use: Mono (default) or CoreCLR.</param>
        /// <param name="processTimeoutMinutes">Maximum time in minutes to wait for a single benchmark process to finish. Default is 10.</param>
        /// <param name="javaScriptEngineArgumentFormatter">Allows to format or customize the arguments passed to the javascript engine.</param>
        public WasmRuntime(
            string msBuildMoniker,
            RuntimeMoniker moniker,
            string displayName,
            bool aot,
            string? javaScriptEngine,
            string? javaScriptEngineArguments = "",
            RuntimeFlavor runtimeFlavor = RuntimeFlavor.Mono,
            int processTimeoutMinutes = 10,
            ArgumentFormatter? javaScriptEngineArgumentFormatter = null) : base(moniker, msBuildMoniker, displayName)
        {
            // Resolve path for windows because we can't use ProcessStartInfo.UseShellExecute while redirecting std out in the executor.
            if (!ProcessHelper.TryResolveExecutableInPath(javaScriptEngine, out javaScriptEngine))
                throw new FileNotFoundException($"Provided {nameof(javaScriptEngine)} file: \"{javaScriptEngine}\" does NOT exist");

            JavaScriptEngine = javaScriptEngine;
            JavaScriptEngineArguments = javaScriptEngineArguments ?? "";
            JavaScriptEngineArgumentFormatter = javaScriptEngineArgumentFormatter ?? DefaultArgumentFormatter;
            RuntimeFlavor = runtimeFlavor;
            IsAOT = aot;
            ProcessTimeoutMinutes = processTimeoutMinutes;
        }

        private WasmRuntime() : base(RuntimeMoniker.WasmNet80, "Wasm", "Wasm")
        {
            IsAOT = RuntimeInformation.IsAot;
            JavaScriptEngine = "";
            JavaScriptEngineArguments = "";
            ProcessTimeoutMinutes = 10;
            JavaScriptEngineArgumentFormatter = DefaultArgumentFormatter;
        }

        public override bool Equals(object? obj)
            => obj is WasmRuntime other && Equals(other);

        public bool Equals(WasmRuntime? other)
        {
            return other != null
                && base.Equals(other)
                && other.JavaScriptEngine == JavaScriptEngine
                && other.JavaScriptEngineArguments == JavaScriptEngineArguments
                && other.JavaScriptEngineArgumentFormatter == JavaScriptEngineArgumentFormatter
                && other.IsAOT == IsAOT
                && other.ProcessTimeoutMinutes == ProcessTimeoutMinutes
                && other.RuntimeFlavor == RuntimeFlavor;
        }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), JavaScriptEngine, JavaScriptEngineArguments, JavaScriptEngineArgumentFormatter, IsAOT, RuntimeFlavor, ProcessTimeoutMinutes);

        private static string DefaultArgumentFormatter(WasmRuntime runtime, ArtifactsPaths artifactsPaths, string args)
        {
            return Path.GetFileNameWithoutExtension(runtime.JavaScriptEngine).ToLower() switch
            {
                "node" or "bun" => $"{runtime.JavaScriptEngineArguments} {artifactsPaths.ExecutablePath} -- --run {artifactsPaths.ProgramName}.dll {args}",
                _ => $"{runtime.JavaScriptEngineArguments} --module {artifactsPaths.ExecutablePath} -- --run {artifactsPaths.ProgramName}.dll {args}",
            };
        }
    }
}