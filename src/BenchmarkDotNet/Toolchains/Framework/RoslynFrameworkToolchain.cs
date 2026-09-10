using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.Roslyn;

namespace BenchmarkDotNet.Toolchains.Framework;

public sealed class RoslynFrameworkToolchain : RoslynToolchain
{
    public static readonly RoslynFrameworkToolchain Default = new(ClrRuntime.GetCurrentVersion());

    private RoslynFrameworkToolchain(ClrRuntime runtime)
        : base("RoslynFramework", runtime, RoslynBuilder.Instance, new Executor()) { }

    /// <summary>Returns a toolchain for the given runtime.</summary>
    public static RoslynFrameworkToolchain From(ClrRuntime runtime)
        => runtime.Equals(Default.Runtime) ? Default : new RoslynFrameworkToolchain(runtime);
}
