using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Toolchains.Wasm;

/// <summary>Toolchain that builds and runs benchmarks on the Mono WebAssembly interpreter.</summary>
public sealed class CsProjMonoWasmToolchain : CsProjWasmToolchain
{
    public static readonly CsProjMonoWasmToolchain Net80 = new(MonoWasmRuntime.Net80, WasmSettings.Default);
    public static readonly CsProjMonoWasmToolchain Net90 = new(MonoWasmRuntime.Net90, WasmSettings.Default);
    public static readonly CsProjMonoWasmToolchain Net10_0 = new(MonoWasmRuntime.Net10_0, WasmSettings.Default);
    public static readonly CsProjMonoWasmToolchain Net11_0 = new(MonoWasmRuntime.Net11_0, WasmSettings.Default);

    private CsProjMonoWasmToolchain(MonoWasmRuntime runtime, WasmSettings settings)
        : base("MonoWasm", runtime, settings, aot: false, useCoreClrRuntime: false) { }

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjMonoWasmToolchain From(MonoWasmRuntime runtime, WasmSettings settings)
    {
        if (!settings.Equals(WasmSettings.Default))
            return new CsProjMonoWasmToolchain(runtime, settings);

        return runtime.Version.Major switch
        {
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new CsProjMonoWasmToolchain(runtime, settings),
        };
    }
}
