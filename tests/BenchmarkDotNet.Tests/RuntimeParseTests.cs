using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Tests;

public class RuntimeParseTests
{
    [Theory]
    // dotted / CLI spellings
    [InlineData("net8.0", typeof(CoreRuntime), 8, 0)]
    [InlineData("net10.0", typeof(CoreRuntime), 10, 0)]
    [InlineData("netcoreapp3.1", typeof(CoreRuntime), 3, 1)]
    [InlineData("net472", typeof(ClrRuntime), 4, 7)]
    [InlineData("net48", typeof(ClrRuntime), 4, 8)]
    [InlineData("nativeaot8.0", typeof(NativeAotRuntime), 8, 0)]
    [InlineData("mono8.0", typeof(MonoCoreRuntime), 8, 0)]
    [InlineData("monowasm8.0", typeof(MonoWasmRuntime), 8, 0)]
    [InlineData("monowasmAot8.0", typeof(MonoWasmAotRuntime), 8, 0)]
    [InlineData("corewasm11.0", typeof(CoreWasmRuntime), 11, 0)]
    [InlineData("r2r8.0", typeof(R2RRuntime), 8, 0)]
    // compact / enum-name spellings
    [InlineData("Net80", typeof(CoreRuntime), 8, 0)]
    [InlineData("Net10_0", typeof(CoreRuntime), 10, 0)]
    [InlineData("NetCoreApp31", typeof(CoreRuntime), 3, 1)]
    [InlineData("NativeAot70", typeof(NativeAotRuntime), 7, 0)]
    [InlineData("Mono60", typeof(MonoCoreRuntime), 6, 0)]
    [InlineData("MonoWasm80", typeof(MonoWasmRuntime), 8, 0)]
    [InlineData("MonoWasmAot80", typeof(MonoWasmAotRuntime), 8, 0)]
    [InlineData("CoreWasm11_0", typeof(CoreWasmRuntime), 11, 0)]
    [InlineData("R2R80", typeof(R2RRuntime), 8, 0)]
    public void ParsesKnownMonikers(string moniker, Type expectedType, int major, int minor)
    {
        Assert.True(Runtime.TryParse(moniker, out var runtime));
        Assert.IsType(expectedType, runtime);
        Assert.Equal(major, runtime!.Version!.Major);
        Assert.Equal(minor, runtime.Version.Minor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hostprocess")]
    [InlineData("notrecognized")]
    [InlineData("garbage")]
    [InlineData("net")]
    // dropped legacy wasm spellings — no longer recognized (only monowasm/monowasmAot/corewasm are)
    [InlineData("wasmnet8.0")]
    [InlineData("WasmNet80")]
    [InlineData("WasmCoreNet11_0")]
    // legacy Mono AOT is versionless; the new Mono AOT has no public toolchain
    [InlineData("monoaot8.0")]
    public void RejectsNonRuntimeMonikers(string moniker)
    {
        Assert.False(Runtime.TryParse(moniker, out var runtime));
        Assert.Null(runtime);
    }

    [Theory]
    [InlineData("net5.0-windows", "net5.0-windows")]
    [InlineData("net8.0-ios", "net8.0-ios")]
    [InlineData("net10.0-windows10.0.19041.0", "net10.0-windows10.0.19041.0")]
    // The platform is never normalized, so the casing survives into the moniker.
    [InlineData("net10.0-WINDOWS", "net10.0-WINDOWS")]
    public void PreservesPlatformSpecificMonikers(string moniker, string expectedTfm)
    {
        Assert.True(Runtime.TryParse(moniker, out var runtime));
        var core = Assert.IsType<CoreRuntime>(runtime);
        Assert.True(core.IsPlatformSpecific);
        Assert.Equal(expectedTfm, core.GetTfm());
    }

    [Theory]
    // The platform is substituted verbatim into the generated project's <TargetFrameworks>: '<' would produce
    // malformed XML and ';' a second target framework, so neither may reach it.
    [InlineData("net8.0-<x>")]
    [InlineData("net8.0-a;net9.0")]
    [InlineData("net8.0-my platform")]
    [InlineData("net8.0-win/dows")]
    [InlineData("net8.0-\"windows\"")]
    [InlineData("net8.0-")]
    [InlineData("net8.0-8.0")] // a version with no platform name
    // Only .NET (Core) carries a platform; the others used to drop the suffix and silently benchmark something else.
    [InlineData("nativeaot8.0-windows")]
    [InlineData("monowasm8.0-browser")]
    [InlineData("net472-windows")]
    // Only .NET 5.0 and later have platform-specific monikers. These have to be reported as unparseable, not throw:
    // TryParse is what the command line calls to decide whether to print its "invalid runtime" message.
    [InlineData("net3.1-windows")]
    [InlineData("net2.1-ios")]
    [InlineData("net3.0-windows10.0.19041.0")]
    [InlineData("netcoreapp3.1-windows")]
    public void RejectsMalformedOrUnsupportedPlatformSuffixes(string moniker)
    {
        Assert.False(Runtime.TryParse(moniker, out var runtime));
        Assert.Null(runtime);
    }

    [Theory]
    // Every runtime whose version identifies a .NET release, not just CoreRuntime: they all take a version straight
    // from Environment.Version somewhere, and their From() switches only normalize the majors they know about.
    [InlineData("nativeaot")]
    [InlineData("r2r")]
    [InlineData("mono")]
    [InlineData("monowasm")]
    [InlineData("monowasmaot")]
    [InlineData("corewasm")]
    public void EveryDotNetRuntimeTrimsAnythingPastMajorMinor(string prefix)
    {
        // A major outside every From() switch, so the uncached fallback is what runs.
        var trimmed = Runtime.Parse($"{prefix}99.0");
        var withBuild = prefix switch
        {
            "nativeaot" => NativeAotRuntime.From(new Version(99, 0, 30)),
            "r2r" => R2RRuntime.From(new Version(99, 0, 30)),
            "mono" => MonoCoreRuntime.From(new Version(99, 0, 30)),
            "monowasm" => MonoWasmRuntime.From(new Version(99, 0, 30)),
            "monowasmaot" => MonoWasmAotRuntime.From(new Version(99, 0, 30)),
            _ => (Runtime) CoreWasmRuntime.From(new Version(99, 0, 30)),
        };

        Assert.Equal(new Version(99, 0), withBuild.Version);
        Assert.Equal(trimmed, withBuild);
        Assert.Equal(trimmed.GetHashCode(), withBuild.GetHashCode());
        Assert.Equal(trimmed.ToString(), withBuild.ToString());
    }

    [Theory]
    // .NET Framework keeps its build: ClrRuntime builds its moniker from it, so 4.8.1 has to stay distinguishable
    // from 4.8. Only the revision is servicing detail there.
    [InlineData("net481", 4, 8, 1)]
    [InlineData("net472", 4, 7, 2)]
    [InlineData("net48", 4, 8, -1)]
    public void ClrRuntimeKeepsItsBuildVersion(string moniker, int major, int minor, int build)
    {
        var runtime = Assert.IsType<ClrRuntime>(Runtime.Parse(moniker));
        Assert.Equal(major, runtime.Version.Major);
        Assert.Equal(minor, runtime.Version.Minor);
        Assert.Equal(build, runtime.Version.Build);
        Assert.Equal(-1, runtime.Version.Revision);
    }

    [Theory]
    // A revision never identifies a .NET Framework release, and GetTfm() ignores it, so keeping it would make two
    // runtimes with the same moniker compare unequal. 4.5 is below the supported floor, so it reaches the uncached
    // fallback in FromVersion rather than a cached instance.
    [InlineData(4, 5, 0, 0)]
    [InlineData(4, 5, 1, 7)]
    public void ClrRuntimeTrimsTheRevision(int major, int minor, int build, int revision)
    {
        var withRevision = ClrRuntime.FromVersion(new Version(major, minor, build, revision));
        var without = ClrRuntime.FromVersion(build > 0 ? new Version(major, minor, build) : new Version(major, minor));

        Assert.Equal(-1, withRevision.Version.Revision);
        Assert.Equal(without, withRevision);
        Assert.Equal(without.GetHashCode(), withRevision.GetHashCode());
        Assert.Equal(without.ToString(), withRevision.ToString());
    }

    [Theory]
    [InlineData("<x>")]
    [InlineData("a;net9.0")]
    [InlineData("my platform")]
    [InlineData("win/dows")]
    [InlineData("8.0")]
    // Every dot has to separate two digits, or the moniker built from it is one MSBuild rejects.
    [InlineData("windows.")]
    [InlineData("windows..0")]
    [InlineData("windows.0")]
    [InlineData("windows10.")]
    public void CoreRuntimeFromRejectsMalformedPlatform(string platform)
    {
        // The parser is not the only way in: CoreRuntime.From is public, and the platform it is given ends up in the
        // generated project's TargetFrameworks just the same.
        var exception = Assert.Throws<ArgumentException>(() => CoreRuntime.From(new Version(8, 0), platform));
        Assert.Equal("platform", exception.ParamName);
    }

    [Theory]
    // Version(8,0), Version(8,0,0) and Version(8,0,0,0) all describe .NET 8.0, and Environment.Version supplies the
    // servicing build (8.0.30) on the auto-detection path, so all four have to collapse to one runtime - otherwise
    // the same runtime compares unequal to itself and jobs are duplicated.
    [InlineData(0, -1)]
    [InlineData(0, 0)]
    [InlineData(30, -1)]
    [InlineData(30, 5)]
    public void TrimsAnythingPastMajorMinor(int build, int revision)
    {
        var version = revision >= 0 ? new Version(8, 0, build, revision)
            : build >= 0 ? new Version(8, 0, build)
            : new Version(8, 0);

        var runtime = CoreRuntime.From(version);
        Assert.Equal(new Version(8, 0), runtime.Version);
        Assert.Equal(".NET 8.0", runtime.ToString());
        Assert.Equal(CoreRuntime.Core80, runtime);
        Assert.Equal(CoreRuntime.Core80.GetHashCode(), runtime.GetHashCode());

        // Same again for the platform-specific path, which does not go through the cached instances.
        var platformSpecific = CoreRuntime.From(version, "windows");
        Assert.Equal(new Version(8, 0), platformSpecific.Version);
        Assert.Equal(CoreRuntime.From(new Version(8, 0), "windows"), platformSpecific);
    }

    [Theory]
    [InlineData("windows")]
    [InlineData("WINDOWS")]
    [InlineData("windows10.0.19041.0")]
    [InlineData("windows10")]
    [InlineData("ios")]
    public void CoreRuntimeFromAcceptsWellFormedPlatform(string platform)
    {
        var runtime = CoreRuntime.From(new Version(8, 0), platform);
        Assert.True(runtime.IsPlatformSpecific);
        Assert.Equal(platform, runtime.Platform);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    public void CoreRuntimeFromRejectsPlatformOnVersionsWithoutPlatformMonikers(int major, int minor)
    {
        // netcoreappX.Y has no platform-specific form, so GetTfm() would drop the platform and silently build for
        // something other than what was asked for.
        var exception = Assert.Throws<ArgumentException>(() => CoreRuntime.From(new Version(major, minor), "windows"));
        Assert.Equal("platform", exception.ParamName);
    }

    [Fact]
    public void ParsesToTheSameInstanceIdentityAsKnownStatics()
    {
        Assert.Equal(ClrRuntime.Net472, Runtime.Parse("net472"));
        // versionless singletons
        Assert.Equal(MonoRuntime.Default, Runtime.Parse("mono"));
        Assert.Equal(MonoAotRuntime.Default, Runtime.Parse("monoaot"));
        Assert.Equal(MonoWasmRuntime.Net80, Runtime.Parse("monowasm8.0"));
        Assert.Equal(MonoWasmAotRuntime.Net80, Runtime.Parse("monowasmAot8.0"));
        // CoreWasm has no cached static (still experimental), so compare against an allocated instance.
        Assert.Equal(CoreWasmRuntime.From(new Version(11, 0)), Runtime.Parse("CoreWasm11_0"));
        Assert.Equal(NativeAotRuntime.Net80, Runtime.Parse("nativeaot8.0"));
    }

    [Theory]
    // Every example the "invalid runtime" error advertises, so the message cannot drift from the parser. If a form is
    // added there, add it here; if one stops parsing, this fails rather than the CLI recommending something invalid.
    [InlineData("net472")]
    [InlineData("net8.0")]
    [InlineData("net8.0-windows")]
    [InlineData("netcoreapp3.1")]
    [InlineData("nativeaot8.0")]
    [InlineData("r2r8.0")]
    [InlineData("mono")]
    [InlineData("mono8.0")]
    [InlineData("monoaot")]
    [InlineData("monowasm8.0")]
    [InlineData("monowasmaot8.0")]
    [InlineData("corewasm11.0")]
    public void EveryMonikerFormTheErrorMessageAdvertisesParses(string moniker)
    {
        Assert.True(Runtime.TryParse(moniker, out var runtime), $"'{moniker}' is advertised by the invalid-runtime error but does not parse.");
        Assert.NotNull(runtime);
    }
}
