using BenchmarkDotNet.Environments;
using System.ComponentModel;

namespace BenchmarkDotNet.Toolchains.Wasm;

/// <summary>Toolchain that builds and runs benchmarks on CoreCLR WebAssembly.</summary>
[EditorBrowsable(EditorBrowsableState.Never)] // WebAssembly with CoreCLR is still experimental. This is only used by dotnet contributors and dotnet/performance until official support is released.
public sealed class CsProjCoreWasmToolchain : CsProjWasmToolchain
{
    private CsProjCoreWasmToolchain(CoreWasmRuntime runtime, WasmSettings settings)
        : base("CoreWasm", runtime, settings, aot: false, useCoreClrRuntime: true) { }

    /// <summary>Creates a toolchain for the given runtime and settings.</summary>
    public static CsProjCoreWasmToolchain From(CoreWasmRuntime runtime, WasmSettings settings)
        => new(runtime, settings);
}
