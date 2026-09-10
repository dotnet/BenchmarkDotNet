using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.NativeAot;

public sealed class CsProjNativeAotToolchain : CsProjNetToolchain
{
    /// <summary>compiled as net7.0</summary>
    public static readonly CsProjNativeAotToolchain Net70 = new(NativeAotRuntime.Net70, NativeAotSettings.Default);
    /// <summary>compiled as net8.0</summary>
    public static readonly CsProjNativeAotToolchain Net80 = new(NativeAotRuntime.Net80, NativeAotSettings.Default);
    /// <summary>compiled as net9.0</summary>
    public static readonly CsProjNativeAotToolchain Net90 = new(NativeAotRuntime.Net90, NativeAotSettings.Default);
    /// <summary>compiled as net10.0</summary>
    public static readonly CsProjNativeAotToolchain Net10_0 = new(NativeAotRuntime.Net10_0, NativeAotSettings.Default);
    /// <summary>compiled as net11.0</summary>
    public static readonly CsProjNativeAotToolchain Net11_0 = new(NativeAotRuntime.Net11_0, NativeAotSettings.Default);

    private CsProjNativeAotToolchain(NativeAotRuntime runtime, NativeAotSettings settings)
        : this(runtime, settings, Resolve(settings, runtime)) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    private CsProjNativeAotToolchain(NativeAotRuntime runtime, NativeAotSettings settings, NativeAotSettings resolved)
        : base("CsProjNativeAot", runtime, settings,
            new CsProjNativeAotGenerator(resolved),
            new DotNetCliPublisher(resolved, GetExtraArguments(settings.RuntimeIdentifier)),
            Toolchains.Executor.Instance)
    {
    }

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise.
    private static NativeAotSettings Resolve(NativeAotSettings settings, NativeAotRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjNativeAotToolchain From(NativeAotRuntime runtime, NativeAotSettings settings)
    {
        if (!settings.Equals(NativeAotSettings.Default))
            return new CsProjNativeAotToolchain(runtime, settings);

        return runtime.Version.Major switch
        {
            7 => Net70,
            8 => Net80,
            9 => Net90,
            10 => Net10_0,
            11 => Net11_0,
            _ => new CsProjNativeAotToolchain(runtime, settings),
        };
    }

    public static string GetExtraArguments(string runtimeIdentifier) => $"-r {runtimeIdentifier}";
}
