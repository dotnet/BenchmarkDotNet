#if !NET
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Tests.Helpers;

public class ShadowCopyHelperTests
{
    // Where the assembly sits says nothing; only being run from somewhere else is a shadow copy.
    // Benchmarks legitimately run from under temp - a file-based app, a repository checked out there.
    [Fact]
    public void AnAssemblyRunFromWhereItWasBuiltIsNotShadowCopied()
    {
        string location = Path.Combine(Path.GetTempPath(), "benchmarks", "Benchmarks.dll");

        Assert.False(ShadowCopyHelper.TryGetOriginalLocation(ToCodeBase(location), location, out _));
    }

    [Fact]
    public void AnAssemblyRunFromSomewhereElseIsShadowCopiedAndResolvesToWhereItWasBuilt()
    {
        string builtTo = Path.Combine(Path.GetTempPath(), "benchmarks", "Benchmarks.dll");
        string runFrom = Path.Combine(Path.GetTempPath(), "cache", "assembly", "dl3", "91598e4b", "dc0f05d4", "Benchmarks.dll");

        Assert.True(ShadowCopyHelper.TryGetOriginalLocation(ToCodeBase(builtTo), runFrom, out string? originalLocation));
        Assert.Equal(builtTo, originalLocation);
    }

    // The code base is a URI and the location a path, so comparing them as strings would report every
    // assembly as shadow copied. The loader also reports the extension in a different case.
    [Fact]
    public void TheCodeBaseAndTheLocationAreComparedAsPaths()
    {
        string location = Path.Combine(Path.GetTempPath(), "benchmarks", "sub", "..", "Benchmarks.dll");

        Assert.False(ShadowCopyHelper.TryGetOriginalLocation(ToCodeBase(location).Replace(".dll", ".DLL"), location, out _));
    }

    // An assembly loaded from a byte array reports neither, which is not a shadow copy either.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void AnAssemblyWithoutAFileIsNotShadowCopied(string? codeBase, string? location)
        => Assert.False(ShadowCopyHelper.TryGetOriginalLocation(codeBase, location, out _));

    [Fact]
    public void AnAssemblyWithANonFileCodeBaseIsNotShadowCopied()
        => Assert.False(ShadowCopyHelper.TryGetOriginalLocation("http://localhost/Benchmarks.dll", Path.Combine(Path.GetTempPath(), "Benchmarks.dll"), out _));

    [Fact]
    public void TheAssemblyRunningTheseTestsIsNotShadowCopied()
        => Assert.False(ShadowCopyHelper.TryGetOriginalLocation(typeof(ShadowCopyHelperTests).Assembly, out _));

    private static string ToCodeBase(string path) => new Uri(path).AbsoluteUri;
}
#endif