using System.Collections.Generic;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.NativeAot;

public sealed record NativeAotSettings : DotNetCliSettings
{
    private const string LocalBuildIlCompilerVersion = "11.0.0-dev";

    public static readonly NativeAotSettings Default = new();

    public string RuntimeIdentifier { get; init; } = RuntimeInformation.GetPortableRuntimeIdentifier();
    public bool UseNuGetClearTag { get; init; }
    public bool UseTempFolderForRestore { get; init; }
    public bool GenerateStackTraceData { get; init; } = true;
    public string OptimizationPreference { get; init; } = "Speed";
    public string InstructionSet { get; init; } = "";
    public string IlCompilerVersion { get; private init; } = "";
    /// <summary>
    /// NuGet feed to restore a specific ILCompiler build from. Mutually exclusive with <see cref="LocalIlcPackages"/>.
    /// For .NET 7+ the ILCompiler is bundled with the SDK, so this is only needed for custom/preview builds.
    /// </summary>
    public string? NuGetFeedUrl { get; private init; }
    /// <summary>Path to a local build of the ILCompiler packages. Mutually exclusive with <see cref="NuGetFeedUrl"/>.</summary>
    public DirectoryInfo? LocalIlcPackages { get; private init; }

    public NativeAotSettings() { }

    internal NativeAotSettings(CommandLineOptions options) : base(options)
    {
        if (options.IlcPackages != null)
        {
            // Restore the ILCompiler from a local runtime build (the generator also adds the dotnet nightly feed).
            LocalIlcPackages = options.IlcPackages;
            IlCompilerVersion = LocalBuildIlCompilerVersion;
            UseTempFolderForRestore = true;
        }
        else if (options.ILCompilerVersion.IsNotBlank())
        {
            IlCompilerVersion = options.ILCompilerVersion;
        }
    }

    /// <summary>
    /// Returns a copy that restores a specific ILCompiler build from a NuGet feed. For .NET 7+ the ILCompiler is bundled
    /// with the SDK (use <see cref="Default"/>), so this is only needed for custom/preview builds.
    /// </summary>
    /// <param name="ilCompilerVersion">the version of Microsoft.DotNet.ILCompiler to use. Empty maps to the latest/bundled version.</param>
    /// <param name="nuGetFeedUrl">the NuGet feed to restore from. The default is "https://api.nuget.org/v3/index.json".</param>
    public NativeAotSettings WithNuGet(string ilCompilerVersion = "", string nuGetFeedUrl = "https://api.nuget.org/v3/index.json") => this with
    {
        IlCompilerVersion = ilCompilerVersion,
        NuGetFeedUrl = nuGetFeedUrl.IsNotBlank() ? nuGetFeedUrl : null,
        LocalIlcPackages = null,
    };

    /// <summary>
    /// Returns a copy that restores the ILCompiler from a local build of the runtime.
    /// See https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/nativeaot.md
    /// </summary>
    /// <param name="ilcPackages">the path to the shipping packages, example: "C:\runtime\artifacts\packages\Release\Shipping"</param>
    public NativeAotSettings WithLocalBuild(DirectoryInfo ilcPackages)
    {
        if (!ilcPackages.Exists)
            throw new DirectoryNotFoundException($"{ilcPackages} provided as {nameof(ilcPackages)} does NOT exist");

        return this with
        {
            LocalIlcPackages = ilcPackages,
            NuGetFeedUrl = null,
            IlCompilerVersion = LocalBuildIlCompilerVersion,
            UseTempFolderForRestore = true,
        };
    }

    /// <inheritdoc />
    public override void FillSettings(IDictionary<string, object?> settings)
    {
        base.FillSettings(settings);
        settings[nameof(RuntimeIdentifier)] = RuntimeIdentifier;
        settings[nameof(UseNuGetClearTag)] = UseNuGetClearTag;
        settings[nameof(UseTempFolderForRestore)] = UseTempFolderForRestore;
        settings[nameof(GenerateStackTraceData)] = GenerateStackTraceData;
        settings[nameof(OptimizationPreference)] = OptimizationPreference;
        settings[nameof(InstructionSet)] = InstructionSet;
        settings[nameof(IlCompilerVersion)] = IlCompilerVersion;
        settings[nameof(NuGetFeedUrl)] = NuGetFeedUrl;
        settings[nameof(LocalIlcPackages)] = LocalIlcPackages?.FullName;
    }

    // DirectoryInfo compares by reference; compare LocalIlcPackages by value and chain into the base record.
    public bool Equals(NativeAotSettings? other)
    {
        if (other is null || !base.Equals(other))
            return false;

        return RuntimeIdentifier == other.RuntimeIdentifier
            && UseNuGetClearTag == other.UseNuGetClearTag
            && UseTempFolderForRestore == other.UseTempFolderForRestore
            && GenerateStackTraceData == other.GenerateStackTraceData
            && OptimizationPreference == other.OptimizationPreference
            && InstructionSet == other.InstructionSet
            && IlCompilerVersion == other.IlCompilerVersion
            && NuGetFeedUrl == other.NuGetFeedUrl
            && LocalIlcPackages?.FullName == other.LocalIlcPackages?.FullName;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(RuntimeIdentifier);
        hashCode.Add(UseNuGetClearTag);
        hashCode.Add(UseTempFolderForRestore);
        hashCode.Add(GenerateStackTraceData);
        hashCode.Add(OptimizationPreference);
        hashCode.Add(InstructionSet);
        hashCode.Add(IlCompilerVersion);
        hashCode.Add(NuGetFeedUrl);
        hashCode.Add(LocalIlcPackages?.FullName);
        return hashCode.ToHashCode();
    }
}
