using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Toolchains.Wasm;

/// <summary>Toolchain that builds and runs benchmarks on Mono WebAssembly AOT.</summary>
public sealed class CsProjMonoWasmAotToolchain : CsProjWasmToolchain
{
    public static readonly CsProjMonoWasmAotToolchain Net80 = new(MonoWasmAotRuntime.Net80, WasmSettings.Default);
    public static readonly CsProjMonoWasmAotToolchain Net90 = new(MonoWasmAotRuntime.Net90, WasmSettings.Default);
    public static readonly CsProjMonoWasmAotToolchain Net10_0 = new(MonoWasmAotRuntime.Net10_0, WasmSettings.Default);
    public static readonly CsProjMonoWasmAotToolchain Net11_0 = new(MonoWasmAotRuntime.Net11_0, WasmSettings.Default);

    private CsProjMonoWasmAotToolchain(MonoWasmAotRuntime runtime, WasmSettings settings)
        : base("MonoWasmAot", runtime, settings, aot: true, useCoreClrRuntime: false) { }

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjMonoWasmAotToolchain From(MonoWasmAotRuntime runtime, WasmSettings settings)
    {
        if (!settings.Equals(WasmSettings.Default))
            return new CsProjMonoWasmAotToolchain(runtime, settings);

        return runtime.Version.Major switch
        {
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new CsProjMonoWasmAotToolchain(runtime, settings),
        };
    }
}
