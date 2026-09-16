using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.Framework;

public sealed class CsProjFrameworkToolchain : Toolchain, IHasSettings
{
    public static readonly CsProjFrameworkToolchain Net461 = new(ClrRuntime.Net461, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net462 = new(ClrRuntime.Net462, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net47 = new(ClrRuntime.Net47, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net471 = new(ClrRuntime.Net471, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net472 = new(ClrRuntime.Net472, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net48 = new(ClrRuntime.Net48, FrameworkSettings.Default);
    public static readonly CsProjFrameworkToolchain Net481 = new(ClrRuntime.Net481, FrameworkSettings.Default);

    private CsProjFrameworkToolchain(ClrRuntime runtime, FrameworkSettings settings)
        : this(runtime, settings, Resolve(settings, runtime)) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    private CsProjFrameworkToolchain(ClrRuntime runtime, FrameworkSettings settings, FrameworkSettings resolved)
        : base("CsProjFramework",
            runtime,
            new CsProjGenerator(resolved, isNetCore: false),
            new DotNetCliBuilder(resolved),
            new Executor())
        => Settings = settings;

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise.
    private static FrameworkSettings Resolve(FrameworkSettings settings, ClrRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    /// <summary>Returns a toolchain for the given runtime and settings.</summary>
    public static CsProjFrameworkToolchain From(ClrRuntime runtime, FrameworkSettings settings)
    {
        if (!settings.Equals(FrameworkSettings.Default))
            return new CsProjFrameworkToolchain(runtime, settings);

        return (runtime.Version.Major, runtime.Version.Minor, runtime.Version.Build) switch
        {
            (4, 8, 1) => Net481,
            (4, 8, _) => Net48,
            (4, 7, 2) => Net472,
            (4, 7, 1) => Net471,
            (4, 7, _) => Net47,
            (4, 6, 2) => Net462,
            (4, 6, 1) => Net461,
            _ => new CsProjFrameworkToolchain(runtime, settings),
        };
    }

    internal FrameworkSettings Settings { get; }

    ISettings IHasSettings.Settings => Settings;

    public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
    {
        await foreach (var validationError in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
        {
            yield return validationError;
        }

        if (!OsDetector.IsWindows())
        {
            yield return new ValidationError(true,
                $"{nameof(CsProjFrameworkToolchain)} is supported only for Windows, benchmark '{benchmarkCase.DisplayInfo}' will not be executed",
                benchmarkCase);
            yield break;
        }
        else if (DotNetSdkValidator.IsCliPathInvalid(((DotNetCliBuilder) Builder).CustomDotNetCliPath, benchmarkCase, out var invalidCliError))
        {
            yield return invalidCliError;
        }

        foreach (var validationError in DotNetSdkValidator.ValidateFrameworkSdks(benchmarkCase))
        {
            yield return validationError;
        }
    }

    public override bool Equals(object? obj)
        => obj is CsProjFrameworkToolchain other
        && Runtime.Equals(other.Runtime)
        && Settings.Equals(other.Settings);

    public override int GetHashCode() => HashCode.Combine(Runtime, Settings);
}
