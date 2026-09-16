using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;

namespace BenchmarkDotNet.Toolchains.DotNetCli;

/// <summary>
/// Settings for toolchains using dotnet cli.
/// </summary>
public abstract record DotNetCliSettings : ISettings
{
    /// <summary>The path to the dotnet cli to use. If null, the system dotnet will be used.</summary>
    public FileInfo? CliPath { get; init; }
    /// <summary>The directory to restore packages to.</summary>
    public DirectoryInfo? PackagesPath { get; init; }
    /// <summary>The target framework moniker to build for. If blank, the toolchain derives it from the runtime.</summary>
    public string TargetFrameworkMoniker { get; init; } = "";

    internal DotNetCliSettings(CommandLineOptions options)
    {
        CliPath = options.CliPath;
        PackagesPath = options.RestorePath;
    }

    protected DotNetCliSettings() { }

    /// <inheritdoc />
    public virtual void FillSettings(IDictionary<string, object?> settings)
    {
        settings[nameof(CliPath)] = CliPath?.FullName;
        settings[nameof(PackagesPath)] = PackagesPath?.FullName;
        settings[nameof(TargetFrameworkMoniker)] = TargetFrameworkMoniker;
    }

    // FileInfo/DirectoryInfo compare by reference, so compare the paths by value here to keep the record - and the
    // toolchain equality built on top of it (used for job deduplication and build partitioning) - value-based.
    // Derived records auto-generate equality that chains into this base implementation.
    public virtual bool Equals(DotNetCliSettings? other)
        => other is not null
            && EqualityContract == other.EqualityContract
            && CliPath?.FullName == other.CliPath?.FullName
            && PackagesPath?.FullName == other.PackagesPath?.FullName
            && TargetFrameworkMoniker == other.TargetFrameworkMoniker;

    public override int GetHashCode()
        => HashCode.Combine(CliPath?.FullName, PackagesPath?.FullName, TargetFrameworkMoniker);
}
