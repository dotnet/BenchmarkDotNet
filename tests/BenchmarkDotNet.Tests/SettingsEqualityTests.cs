using System;
using System.IO;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.R2R;
using BenchmarkDotNet.Toolchains.Wasm;

namespace BenchmarkDotNet.Tests;

// FileInfo/DirectoryInfo/Func members compare by reference by default. The *Settings records override equality to
// compare paths by value (FullName), so two settings built from distinct FileInfo instances pointing at the same path
// must be equal (and share a hash code) - otherwise toolchains built on them would defeat job dedup/build partitioning.
public class SettingsEqualityTests
{
    private const string PathA = "some/dir/tool.exe";
    private const string PathB = "some/other/tool.exe";

    private static void AssertEqualWithHash<T>(T x, T y) where T : notnull
    {
        Assert.Equal(x, y);
        Assert.Equal(x.GetHashCode(), y.GetHashCode());
    }

    [Fact]
    public void R2RSettings_ComparePacksByPath()
    {
        var a = new R2RSettings { Crossgen2Pack = new FileInfo(PathA) };
        var sameAsA = new R2RSettings { Crossgen2Pack = new FileInfo(PathA) };
        var differs = new R2RSettings { Crossgen2Pack = new FileInfo(PathB) };

        AssertEqualWithHash(a, sameAsA);
        Assert.NotEqual(a, differs);
    }

    [Fact]
    public void WasmSettings_CompareMainJsTemplateByPath_AndDefaultFormatterEqual()
    {
        var a = new WasmSettings { MainJsTemplate = new FileInfo(PathA) };
        var sameAsA = new WasmSettings { MainJsTemplate = new FileInfo(PathA) };
        var differs = new WasmSettings { MainJsTemplate = new FileInfo(PathB) };

        AssertEqualWithHash(a, sameAsA);
        Assert.NotEqual(a, differs);
        // Two fresh instances use the same static default formatter, so they must be equal.
        AssertEqualWithHash(new WasmSettings(), new WasmSettings());
    }

    [Fact]
    public void CoreRunSettings_CompareSourceCoreRunByPath()
    {
        var a = new CoreRunSettings { SourceCoreRun = new FileInfo(PathA) };
        var sameAsA = new CoreRunSettings { SourceCoreRun = new FileInfo(PathA) };
        var differs = new CoreRunSettings { SourceCoreRun = new FileInfo(PathB) };

        AssertEqualWithHash(a, sameAsA);
        Assert.NotEqual(a, differs);
    }

    [Fact]
    public void LegacyMonoSettings_CompareMonoPathByPath()
    {
        var a = new MonoSettings { MonoPath = new FileInfo(PathA) };
        var sameAsA = new MonoSettings { MonoPath = new FileInfo(PathA) };
        var differs = new MonoSettings { MonoPath = new FileInfo(PathB) };

        AssertEqualWithHash(a, sameAsA);
        Assert.NotEqual(a, differs);

        // Derived AOT record chains into the base path comparison and adds its own AotArgs.
        var aot = new MonoAotSettings { MonoPath = new FileInfo(PathA), AotArgs = "--aot=full" };
        var aotSame = new MonoAotSettings { MonoPath = new FileInfo(PathA), AotArgs = "--aot=full" };
        var aotDiffers = new MonoAotSettings { MonoPath = new FileInfo(PathA), AotArgs = "--aot" };

        AssertEqualWithHash(aot, aotSame);
        Assert.NotEqual(aot, aotDiffers);
    }

    [Fact]
    public void NativeAotSettings_CompareLocalIlcPackagesByPath()
    {
        // WithLocalBuild requires the directory to exist, so use the test output directory.
        var existing = AppContext.BaseDirectory;

        var a = NativeAotSettings.Default.WithLocalBuild(new DirectoryInfo(existing));
        var sameAsA = NativeAotSettings.Default.WithLocalBuild(new DirectoryInfo(existing));

        AssertEqualWithHash(a, sameAsA);
        Assert.NotEqual(a, NativeAotSettings.Default);
    }
}
