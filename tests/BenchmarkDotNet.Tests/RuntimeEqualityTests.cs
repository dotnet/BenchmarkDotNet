using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Tests;

public class RuntimeEqualityTests
{
    // A custom runtime with equality state (Extra) beyond Name/Version, overriding the polymorphic Equals(object).
    private sealed class CustomRuntime(string name, Version version, string extra) : Runtime
    {
        public override string Name => name;
        public override Version? Version => version;
        private string Extra { get; } = extra;

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase) => throw new NotImplementedException();
        public override bool Equals(object? obj) => obj is CustomRuntime other && base.Equals(other) && Extra == other.Extra;
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Extra);
    }

    [Fact]
    public void CustomRuntimeEqualityIsHonoredThroughTheIEquatableRuntimePath()
    {
        Runtime a = new CustomRuntime("Custom", new Version(1, 0), "a");
        Runtime differsByExtra = new CustomRuntime("Custom", new Version(1, 0), "b");
        Runtime sameAsA = new CustomRuntime("Custom", new Version(1, 0), "a");

        // Directly via the concrete Equals(object).
        Assert.False(a.Equals((object)differsByExtra));
        Assert.True(a.Equals((object)sameAsA));

        // Via IEquatable<Runtime> (Dictionary/HashSet/EqualityComparer use this) — the path that previously ignored Extra.
        var comparer = EqualityComparer<Runtime>.Default;
        Assert.False(comparer.Equals(a, differsByExtra));
        Assert.True(comparer.Equals(a, sameAsA));

        var dictionary = new Dictionary<Runtime, int> { [a] = 1 };
        Assert.False(dictionary.ContainsKey(differsByExtra));
        Assert.True(dictionary.ContainsKey(sameAsA));
    }
}
