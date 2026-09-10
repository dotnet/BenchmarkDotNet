using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.R2R;

public sealed record R2RSettings : DotNetCliSettings
{
    public static readonly R2RSettings Default = new();

    /// <summary>Optional path to a custom runtime pack.</summary>
    public DirectoryInfo? CustomRuntimePack { get; init; }
    /// <summary>Optional path to the Crossgen2 pack.</summary>
    public FileInfo? Crossgen2Pack { get; init; }

    public R2RSettings() { }

    internal R2RSettings(CommandLineOptions options) : base(options)
    {
        CustomRuntimePack = options.CustomRuntimePack;
        Crossgen2Pack = options.AOTCompilerPath;
    }

    /// <inheritdoc />
    public override void FillSettings(IDictionary<string, object?> settings)
    {
        base.FillSettings(settings);
        settings[nameof(CustomRuntimePack)] = CustomRuntimePack?.FullName;
        settings[nameof(Crossgen2Pack)] = Crossgen2Pack?.FullName;
    }

    // FileInfo/DirectoryInfo compare by reference; compare the declared paths by value and chain into the base record.
    public bool Equals(R2RSettings? other)
        => other is not null
            && base.Equals(other)
            && CustomRuntimePack?.FullName == other.CustomRuntimePack?.FullName
            && Crossgen2Pack?.FullName == other.Crossgen2Pack?.FullName;

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), CustomRuntimePack?.FullName, Crossgen2Pack?.FullName);
}
