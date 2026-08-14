using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Toolchains;

/// <summary>
/// Derives the target framework moniker (e.g. <c>net8.0</c>, <c>net472</c>, <c>netcoreapp3.1</c>) that SDK-based
/// toolchains build against. This is a build detail owned by the toolchain layer, not a property of the runtime.
/// Callers bind to the overload matching their statically-known runtime type — no runtime type dispatch — so a
/// toolchain that already holds a concrete runtime gets the right moniker directly; the <see cref="Runtime"/>
/// overload is the fallback for the runtimes whose default moniker is just <c>net{Major}.{Minor}</c>.
/// </summary>
internal static class RuntimeTfmExtensions
{
    internal static string GetTfm(this ClrRuntime runtime)
        => runtime.Version.Build > 0
            ? $"net{runtime.Version.Major}{runtime.Version.Minor}{runtime.Version.Build}"
            : $"net{runtime.Version.Major}{runtime.Version.Minor}";

    internal static string GetTfm(this CoreRuntime runtime)
        // .NET Core < 5 uses the netcoreappX.Y moniker; 5+ never has a non-zero minor version.
        => runtime.Version.Major < 5 ? $"netcoreapp{runtime.Version.Major}.{runtime.Version.Minor}"
         : runtime.IsPlatformSpecific ? $"net{runtime.Version.Major}.0-{runtime.Platform}"
         : $"net{runtime.Version.Major}.0";

    internal static string GetTfm(this Runtime runtime)
        => $"net{runtime.Version!.Major}.{runtime.Version.Minor}";
}
