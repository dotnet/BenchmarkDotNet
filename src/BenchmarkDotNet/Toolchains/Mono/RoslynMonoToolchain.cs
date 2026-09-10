using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Roslyn;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.Mono;

public sealed class RoslynMonoToolchain : RoslynToolchain, IHasSettings
{
    public static readonly RoslynMonoToolchain Default = new(MonoSettings.Default);

    private RoslynMonoToolchain(MonoSettings settings)
        : base("RoslynMono", MonoRuntime.Default, RoslynBuilder.Instance, new LegacyMonoExecutor(settings))
        => Settings = settings;

    /// <summary>Returns a toolchain for the given settings.</summary>
    public static RoslynMonoToolchain From(MonoSettings settings)
        => settings.Equals(MonoSettings.Default) ? Default : new RoslynMonoToolchain(settings);

    internal MonoSettings Settings { get; }

    ISettings IHasSettings.Settings => Settings;

    public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
    {
        await foreach (var validationError in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
        {
            yield return validationError;
        }

        if (Settings.MonoBclPath is { Exists: false })
        {
            yield return new ValidationError(true,
                $"MonoBclPath provided for {nameof(RoslynMonoToolchain)}: \"{Settings.MonoBclPath}\" does NOT exist.",
                benchmarkCase);
        }

        if (Settings.MonoPath is null && !HostEnvironmentInfo.GetCurrent().IsMonoInstalled.Value)
        {
            yield return new ValidationError(true,
                $"Mono is not installed or added to PATH",
                benchmarkCase);
        }
    }

    public override bool Equals(object? obj)
        => obj is RoslynMonoToolchain other
        && Runtime.Equals(other.Runtime)
        && Settings.Equals(other.Settings);

    public override int GetHashCode() => HashCode.Combine(Runtime, Settings);
}
