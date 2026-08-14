using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public abstract class WasmRuntime(Version version) : Runtime
    {
        public override Version Version { get; } = version;

        // Resolves the concrete WebAssembly runtime for the current process. CoreCLR vs Mono is detected reliably via
        // RuntimeInformation.IsMono; the version comes from Environment.Version like the other Mono-based runtimes.
        // Interpreter vs AOT can NOT be detected at runtime - AOT is a publish-time setting and AOT'd wasm still bundles
        // the interpreter, so IsAot/RuntimeFeature report the same for both - so Mono wasm defaults to the interpreter
        // runtime here. When AOT is used the job's toolchain (CsProjMonoWasmAotToolchain) supplies the authoritative runtime.
        internal static WasmRuntime GetCurrentVersion()
            => RuntimeInformation.IsMono
                ? MonoWasmRuntime.From(Environment.Version)
                : CoreWasmRuntime.From(Environment.Version);
    }
}
