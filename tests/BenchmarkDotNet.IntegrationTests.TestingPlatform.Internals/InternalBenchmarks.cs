using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// A benchmark class that is not visible outside its assembly. Nothing stops one being declared, and
    /// ContainsRunnableBenchmarks does not filter by visibility, so its benchmarks are enumerated and reported like
    /// any other - CompilationValidator rejects them later, by which time their nodes have been published.
    /// </summary>
    internal class InternalBenchmarks
    {
        [Benchmark]
        public int Identity() => 1;
    }

    /// <summary>
    /// The same, deriving from a visible benchmark class: the first base type that is visible is one that declares
    /// benchmarks of its own, so a name taken from there belongs to a type that is reported separately.
    /// </summary>
    internal class DerivedInternalBenchmarks : VisibleBenchmarks
    {
    }

    public class VisibleBenchmarks
    {
        [Benchmark]
        public int Identity() => 2;
    }
}
