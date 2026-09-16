using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;

namespace BenchmarkDotNet.Toolchains.Mono;

public sealed record MonoAotSettings : LegacyMonoSettings
{
    public static readonly MonoAotSettings Default = new();

    public MonoAotSettings() { }

    internal MonoAotSettings(CommandLineOptions options) : base(options) { }

    /// <summary>
    /// Aot args for the build.
    /// Example: "--aot=full,llvm". See https://www.mono-project.com/docs/advanced/aot/ for more details.
    /// </summary>
    public string AotArgs { get; init; } = "--aot";

    /// <inheritdoc />
    public override void FillSettings(IDictionary<string, object?> settings)
    {
        base.FillSettings(settings);
        settings[nameof(AotArgs)] = AotArgs;
    }
}
