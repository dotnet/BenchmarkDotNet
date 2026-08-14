using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.R2R;

public sealed class CsProjR2RToolchain : CsProjNetToolchain
{
    public static readonly CsProjR2RToolchain R2R80 = new(R2RRuntime.Net80, R2RSettings.Default);
    public static readonly CsProjR2RToolchain R2R90 = new(R2RRuntime.Net90, R2RSettings.Default);
    public static readonly CsProjR2RToolchain R2R10_0 = new(R2RRuntime.Net10_0, R2RSettings.Default);
    public static readonly CsProjR2RToolchain R2R11_0 = new(R2RRuntime.Net11_0, R2RSettings.Default);

    private CsProjR2RToolchain(R2RRuntime runtime, R2RSettings settings)
        : this(runtime, settings, Resolve(settings, runtime)) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    private CsProjR2RToolchain(R2RRuntime runtime, R2RSettings settings, R2RSettings resolved)
        : base("CsProjR2R", runtime, settings,
            new R2RGenerator(resolved),
            new DotNetCliPublisher(resolved),
            Toolchains.Executor.Instance)
    {
    }

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise.
    private static R2RSettings Resolve(R2RSettings settings, R2RRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjR2RToolchain From(R2RRuntime runtime, R2RSettings settings)
    {
        if (!settings.Equals(R2RSettings.Default))
            return new CsProjR2RToolchain(runtime, settings);

        return runtime.Version.Major switch
        {
            8 => R2R80,
            9 => R2R90,
            10 => R2R10_0,
            11 => R2R11_0,
            _ => new CsProjR2RToolchain(runtime, settings),
        };
    }
}
