using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.NetCoreApp;

public sealed class CsProjCoreToolchain : CsProjNetToolchain
{
    public static readonly CsProjCoreToolchain NetCoreApp20 = new(CoreRuntime.Core20, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp21 = new(CoreRuntime.Core21, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp22 = new(CoreRuntime.Core22, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp30 = new(CoreRuntime.Core30, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp31 = new(CoreRuntime.Core31, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp50 = new(CoreRuntime.Core50, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp60 = new(CoreRuntime.Core60, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp70 = new(CoreRuntime.Core70, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp80 = new(CoreRuntime.Core80, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp90 = new(CoreRuntime.Core90, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp10_0 = new(CoreRuntime.Core10_0, NetCoreAppSettings.Default);
    public static readonly CsProjCoreToolchain NetCoreApp11_0 = new(CoreRuntime.Core11_0, NetCoreAppSettings.Default);

    private CsProjCoreToolchain(CoreRuntime runtime, NetCoreAppSettings settings)
        : this(runtime, settings, Resolve(settings, runtime)) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    private CsProjCoreToolchain(Runtime runtime, NetCoreAppSettings settings, NetCoreAppSettings resolved)
        : base("CsProjCore", runtime, settings,
            new CsProjGenerator(resolved),
            new DotNetCliBuilder(resolved),
            new DotNetCliExecutor(settings.CliPath))
    {
    }

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise. Typed to CoreRuntime so its netcoreappX.Y / platform-specific
    // GetTfm binds (the MonoCoreRuntime path uses default settings and resolves the moniker inline instead).
    private static NetCoreAppSettings Resolve(NetCoreAppSettings settings, CoreRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjCoreToolchain From(CoreRuntime runtime, NetCoreAppSettings settings)
    {
        // Platform-specific runtimes (e.g. net8.0-windows) must keep the platform in the generated TFM,
        // so they can't reuse the platform-agnostic cached toolchains.
        if (!settings.Equals(NetCoreAppSettings.Default) || runtime.IsPlatformSpecific)
            return new CsProjCoreToolchain(runtime, settings);

        return (runtime.Version.Major, runtime.Version.Minor) switch
        {
            (2, 0) => NetCoreApp20,
            (2, 1) => NetCoreApp21,
            (2, 2) => NetCoreApp22,
            (3, 0) => NetCoreApp30,
            (3, 1) => NetCoreApp31,
            (5, 0) => NetCoreApp50,
            (6, 0) => NetCoreApp60,
            (7, 0) => NetCoreApp70,
            (8, 0) => NetCoreApp80,
            (9, 0) => NetCoreApp90,
            (10, 0) => NetCoreApp10_0,
            (11, 0) => NetCoreApp11_0,
            _ => new CsProjCoreToolchain(runtime, settings),
        };
    }

    // A .NET SDK with Mono as the default VM uses this plain-dotnet toolchain, but keeps the job's MonoCoreRuntime
    // so IToolchain.Runtime matches. See MonoCoreRuntime.GetDefaultToolchain. Always uses default settings, so the
    // resolved moniker (plain net{Major}.0 from the base Runtime.GetTfm) is built inline.
    internal static CsProjCoreToolchain From(MonoCoreRuntime runtime)
        => new(runtime, NetCoreAppSettings.Default, new NetCoreAppSettings { TargetFrameworkMoniker = runtime.GetTfm() });
}
