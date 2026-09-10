using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;

namespace BenchmarkDotNet.Toolchains.Mono;

/// <summary>Settings for legacy Mono toolchains.</summary>
public abstract record LegacyMonoSettings : ISettings
{
    /// <summary>Optional path to the Mono installation.</summary>
    public FileInfo? MonoPath { get; init; }
    /// <summary>Optional path to the Mono Base Class Library (BCL) directory.</summary>
    public DirectoryInfo? MonoBclPath { get; init; }

    protected LegacyMonoSettings() { }

    internal LegacyMonoSettings(CommandLineOptions options)
    {
        MonoPath = options.MonoPath;
    }

    /// <inheritdoc />
    public virtual void FillSettings(IDictionary<string, object?> settings)
    {
        settings[nameof(MonoPath)] = MonoPath?.FullName;
        settings[nameof(MonoBclPath)] = MonoBclPath?.FullName;
    }

    // FileInfo/DirectoryInfo compare by reference, so compare the paths by value here to keep the record - and the
    // toolchain equality built on top of it (used for job deduplication and build partitioning) - value-based.
    // Derived records auto-generate equality that chains into this base implementation.
    public virtual bool Equals(LegacyMonoSettings? other)
        => other is not null
            && EqualityContract == other.EqualityContract
            && MonoPath?.FullName == other.MonoPath?.FullName
            && MonoBclPath?.FullName == other.MonoBclPath?.FullName;

    public override int GetHashCode()
        => HashCode.Combine(MonoPath?.FullName, MonoBclPath?.FullName);
}
