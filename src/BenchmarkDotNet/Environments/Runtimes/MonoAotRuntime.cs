using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.Environments;

/// <summary>
/// The legacy (based on .Net Framework) Mono runtime, AOT-compiled via <c>mono --aot</c>.
/// </summary>
public sealed class MonoAotRuntime : LegacyMonoRuntime
{
    public static readonly MonoAotRuntime Default = new();

    public override string Name => "MonoAot";

    private MonoAotRuntime() { }

    public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        => RoslynMonoAotToolchain.Default;
}
