using Microsoft.CodeAnalysis.Testing;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public abstract class TestFixtureBase
{
    // TODO: Remove temporary workaround code after issue resolved. https://github.com/dotnet/roslyn/issues/86477
    private static readonly Lazy<ReferenceAssemblies> LazyNet110 = new(()
        => new ReferenceAssemblies(
            targetFramework: "net11.0",
            referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "11.0.0"),
            referenceAssemblyPath: Path.Combine("ref", "net11.0")));

    protected static ReferenceAssemblies GetReferenceAssemblies()
    {
#if NET11_0_OR_GREATER
        return LazyNet110.Value; // TODO: Replace to ReferenceAssemblies.Net.Net110;
#else
        return ReferenceAssemblies.NetStandard.NetStandard20;
#endif
    }

    protected static void AddAdditionalReferences(SolutionState testState)
    {
        testState.AdditionalReferences.Add("BenchmarkDotNet.dll");
        testState.AdditionalReferences.Add("BenchmarkDotNet.Annotations.dll");
#if !NET6_0_OR_GREATER
        testState.AdditionalReferences.Add("System.Memory.dll");
        // Polyfills `IAsyncEnumerable<T>` etc. on netstandard2.0.
        testState.AdditionalReferences.Add("Microsoft.Bcl.AsyncInterfaces.dll");
#endif
    }
}
