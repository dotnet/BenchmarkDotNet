using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Wasm;

namespace BenchmarkDotNet.Environments;

/// <summary>
/// The Mono WebAssembly runtime running in interpreter mode (including the Jiterpreter).
/// </summary>
public sealed class MonoWasmRuntime : WasmRuntime
{
    public static readonly MonoWasmRuntime Net80 = new(new(8, 0));
    public static readonly MonoWasmRuntime Net90 = new(new(9, 0));
    public static readonly MonoWasmRuntime Net10_0 = new(new(10, 0));
    public static readonly MonoWasmRuntime Net11_0 = new(new(11, 0));

    private MonoWasmRuntime(Version version) : base(version) { }

    public override string Name => "MonoWasm";

    /// <summary>Returns a runtime for the given version.</summary>
    public static MonoWasmRuntime From(Version version)
        => version.Major switch
        {
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new(version),
        };

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => CsProjMonoWasmToolchain.From(this, WasmSettings.Default);
}
