using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Environments;

public sealed class UnknownRuntime : Runtime
{
    public static readonly UnknownRuntime Instance = new();

    private UnknownRuntime() { }

    public override string Name => "?";

    public override Version? Version => null;

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => throw new NotSupportedException($"A default toolchain cannot be determined for {nameof(UnknownRuntime)}.");
}
