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
        /// When true (default), the generated project uses Microsoft.NET.Sdk.WebAssembly which sets UseMonoRuntime=true
        /// and resolves the Mono runtime pack (Microsoft.NETCore.App.Runtime.Mono.browser-wasm). When false, the generated
        /// project uses Microsoft.NET.Sdk which resolves the CoreCLR runtime pack (Microsoft.NETCore.App.Runtime.browser-wasm).
        /// </summary>
        public bool IsMonoRuntime { get; }

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
        /// <param name="isMonoRuntime">When true (default), use Mono runtime pack; when false, use CoreCLR runtime pack.</param>
        /// <param name="javaScriptEngineArguments">Arguments for the javascript engine.</param>
        /// <param name="processTimeoutMinutes">Maximum time in minutes to wait for a single benchmark process to finish. Default is 10.</param>
        /// <param name="javaScriptEngineArgumentFormatter">Allows to format or customize the arguments passed to the javascript engine.</param>
        public WasmRuntime(
            string msBuildMoniker,
            RuntimeMoniker moniker,
            string displayName,
            bool aot,
            string? javaScriptEngine,
            bool isMonoRuntime = true,
            string? javaScriptEngineArguments = "",
            int processTimeoutMinutes = 10,
            ArgumentFormatter? javaScriptEngineArgumentFormatter = null) : base(moniker, msBuildMoniker, displayName)
        {
            // Resolve path for windows because we can't use ProcessStartInfo.UseShellExecute while redirecting std out in the executor.
            if (!ProcessHelper.TryResolveExecutableInPath(javaScriptEngine, out javaScriptEngine))
                throw new FileNotFoundException($"Provided {nameof(javaScriptEngine)} file: \"{javaScriptEngine}\" does NOT exist");

            JavaScriptEngine = javaScriptEngine;
            JavaScriptEngineArguments = javaScriptEngineArguments ?? "";
            JavaScriptEngineArgumentFormatter = javaScriptEngineArgumentFormatter ?? DefaultArgumentFormatter;
            IsMonoRuntime = isMonoRuntime;
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
            => other != null && base.Equals(other) && other.JavaScriptEngine == JavaScriptEngine && other.JavaScriptEngineArguments == JavaScriptEngineArguments && other.IsAOT == IsAOT && other.IsMonoRuntime == IsMonoRuntime;

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), JavaScriptEngine, JavaScriptEngineArguments, IsAOT, IsMonoRuntime);

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