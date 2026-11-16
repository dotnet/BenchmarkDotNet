using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using BenchmarkDotNet.Toolchains.R2R;
using BenchmarkDotNet.Toolchains.Wasm;

namespace BenchmarkDotNet.IntegrationTests;

internal static class TestDefaults
{
    public static CsProjCoreToolchain CsProjCoreToolchain
        => CsProjCoreToolchain.NetCoreApp11_0;

    public static CsProjR2RToolchain CsProjR2RToolchain
        => CsProjR2RToolchain.R2R11_0;

    public static CsProjNativeAotToolchain CsProjNativeAotToolchain
        => CsProjNativeAotToolchain.Net11_0;

    public static CoreRuntime CoreRuntime
        => CoreRuntime.Core11_0;

    public const string CoreWasm = "corewasm11.0"; // Currently `RuntimeMoniker CoreWasm11_0` is not exposed.

    public static CsProjMonoWasmToolchain CsProjMonoWasmToolchain => CsProjMonoWasmToolchain.Net11_0;

    public static MonoWasmRuntime MonoWasmRuntime => MonoWasmRuntime.Net11_0;

    public const string MonoWasm = RuntimeMoniker.MonoWasm11_0;

    public const string MonoWasmAot = RuntimeMoniker.MonoWasmAot11_0;
}
