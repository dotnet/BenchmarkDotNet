using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Wasm;

namespace BenchmarkDotNet.Environments;

/// <summary>
/// The Mono WebAssembly runtime AOT-compiled.
/// </summary>
public sealed class MonoWasmAotRuntime : WasmRuntime
{
    public static readonly MonoWasmAotRuntime Net80 = new(new(8, 0));
    public static readonly MonoWasmAotRuntime Net90 = new(new(9, 0));
    public static readonly MonoWasmAotRuntime Net10_0 = new(new(10, 0));
    public static readonly MonoWasmAotRuntime Net11_0 = new(new(11, 0));

    private MonoWasmAotRuntime(Version version) : base(version) { }

    public override string Name => "MonoWasmAot";

    /// <summary>Returns a runtime for the given version.</summary>
    public static MonoWasmAotRuntime From(Version version)
        => version.Major switch
        {
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new(version),
        };

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => CsProjMonoWasmAotToolchain.From(this, WasmSettings.Default);
}
