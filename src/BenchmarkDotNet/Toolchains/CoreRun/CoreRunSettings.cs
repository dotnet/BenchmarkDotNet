using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRun;

/// <summary>Settings for the <see cref="CoreRunToolchain"/>.</summary>
public sealed record CoreRunSettings : DotNetCliSettings
{
    /// <summary>
    /// Path to CoreRun.exe (corerun on Unix). BDN expects the path to CoreRun itself, not the Core_Root folder.
    /// </summary>
    public required FileInfo SourceCoreRun { get; init; }

    /// <summary>
    /// Whether to shadow-copy CoreRun (true by default). The toolchain replaces old dependencies in the CoreRun
    /// folder with newer versions used by the benchmarks, so copying avoids mutating the original.
    /// </summary>
    public bool CreateCopy { get; init; } = true;

    /// <summary>Display name for the toolchain ("CoreRun" by default).</summary>
    public string DisplayName { get; init; } = "CoreRun";

    public CoreRunSettings() { }

    internal CoreRunSettings(CommandLineOptions options) : base(options) { }

    /// <inheritdoc />
    public override void FillSettings(IDictionary<string, object?> settings)
    {
        base.FillSettings(settings);
        settings[nameof(SourceCoreRun)] = SourceCoreRun.FullName;
        settings[nameof(CreateCopy)] = CreateCopy;
        settings[nameof(DisplayName)] = DisplayName;
    }

    // FileInfo compares by reference; compare SourceCoreRun by value and chain into the base record.
    public bool Equals(CoreRunSettings? other)
        => other is not null
            && base.Equals(other)
            && SourceCoreRun?.FullName == other.SourceCoreRun?.FullName
            && CreateCopy == other.CreateCopy
            && DisplayName == other.DisplayName;

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), SourceCoreRun?.FullName, CreateCopy, DisplayName);
}
