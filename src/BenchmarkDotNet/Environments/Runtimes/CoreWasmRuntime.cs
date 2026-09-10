using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Wasm;
using System.ComponentModel;

namespace BenchmarkDotNet.Environments;

/// <summary>
/// The CoreCLR WebAssembly runtime.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)] // WebAssembly with CoreCLR is still experimental. This is only used by dotnet contributors and dotnet/performance until official support is released.
public sealed class CoreWasmRuntime : WasmRuntime
{
    private CoreWasmRuntime(Version version) : base(version) { }

    public override string Name => "CoreWasm";

    /// <summary>Returns a runtime for the given version.</summary>
    // No cached versions yet (still experimental), so this always allocates.
    public static CoreWasmRuntime From(Version version) => new(version);

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => CsProjCoreWasmToolchain.From(this, WasmSettings.Default);
}
