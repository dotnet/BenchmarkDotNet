using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.Mono;

public sealed class CsProjMonoCoreToolchain : CsProjNetToolchain
{
    public static readonly CsProjMonoCoreToolchain Mono60 = new(MonoCoreRuntime.Net60, MonoCoreSettings.Default);
    public static readonly CsProjMonoCoreToolchain Mono70 = new(MonoCoreRuntime.Net70, MonoCoreSettings.Default);
    public static readonly CsProjMonoCoreToolchain Mono80 = new(MonoCoreRuntime.Net80, MonoCoreSettings.Default);
    public static readonly CsProjMonoCoreToolchain Mono90 = new(MonoCoreRuntime.Net90, MonoCoreSettings.Default);
    public static readonly CsProjMonoCoreToolchain Mono10_0 = new(MonoCoreRuntime.Net10_0, MonoCoreSettings.Default);
    public static readonly CsProjMonoCoreToolchain Mono11_0 = new(MonoCoreRuntime.Net11_0, MonoCoreSettings.Default);

    private CsProjMonoCoreToolchain(MonoCoreRuntime runtime, MonoCoreSettings settings)
        : this(runtime, settings, Resolve(settings, runtime)) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    private CsProjMonoCoreToolchain(MonoCoreRuntime runtime, MonoCoreSettings settings, MonoCoreSettings resolved)
        : base("Mono", runtime, settings,
            new CsProjMonoGenerator(resolved),
            new MonoPublisher(resolved),
            new DotNetCliExecutor(settings.CliPath))
    {
    }

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise.
    private static MonoCoreSettings Resolve(MonoCoreSettings settings, MonoCoreRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjMonoCoreToolchain From(MonoCoreRuntime runtime, MonoCoreSettings settings)
    {
        if (!settings.Equals(MonoCoreSettings.Default))
            return new CsProjMonoCoreToolchain(runtime, settings);

        return runtime.Version.Major switch
        {
            6 => Mono60,
            7 => Mono70,
            8 => Mono80,
            9 => Mono90,
            10 => Mono10_0,
            11 => Mono11_0,
            _ => new CsProjMonoCoreToolchain(runtime, settings),
        };
    }
}
