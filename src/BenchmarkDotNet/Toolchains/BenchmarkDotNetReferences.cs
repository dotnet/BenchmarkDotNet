using System.Reflection;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Toolchains;

internal static class BenchmarkDotNetReferences
{
    // BenchmarkDotNet and the (non-framework) libraries it brings that the generated benchmark is compiled against,
    // besides the project that defines the benchmarks. Shared between the Roslyn toolchain - which builds its own
    // reference set (see Roslyn.Generator.GetAllReferences) - and the build-error explanation - which uses it to
    // recognize a missing BenchmarkDotNet reference (see MsBuildErrorMapper). Keeping them in one place means that if
    // code generation starts depending on another BenchmarkDotNet library, both stay in sync.
    internal static readonly IReadOnlyList<Assembly> Assemblies =
    [
        typeof(BenchmarkCase).Assembly, // BenchmarkDotNet
        typeof(Perfolizer.Horology.IClock).Assembly // Perfolizer
    ];

    // Types the generated benchmark is compiled against that live in a framework assembly on .NET Core but come from a
    // NuGet package (System.Threading.Tasks.Extensions) on .NET Framework, so they can go missing the same way. Kept as
    // specific types rather than their assembly: on .NET Core that assembly is the whole BCL, which must not be treated
    // as part of the BenchmarkDotNet closure.
    internal static readonly IReadOnlyList<Type> Types =
    [
        typeof(ValueTask), // System.Threading.Tasks.Extensions on .NET Framework
        typeof(ValueTask<>)
    ];
}
