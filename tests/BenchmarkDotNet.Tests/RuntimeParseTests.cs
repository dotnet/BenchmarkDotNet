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
    public void PreservesPlatformSpecificMonikers(string moniker, string expectedTfm)
    {
        Assert.True(Runtime.TryParse(moniker, out var runtime));
        var core = Assert.IsType<CoreRuntime>(runtime);
        Assert.True(core.IsPlatformSpecific);
        Assert.Equal(expectedTfm, core.GetTfm());
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
}
