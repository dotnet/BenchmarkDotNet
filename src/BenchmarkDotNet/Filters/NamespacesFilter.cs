using System.Linq;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// filters benchmarks by provided namespaces
    /// </summary>
    public class NamespacesFilter : IFilter
    {
        private readonly string[] namespaces;

        public NamespacesFilter(string[] namespaces) => this.namespaces = namespaces;

        public bool Predicate(Benchmark benchmark) => namespaces.Any(@namespace => benchmark.Target.Type.Namespace.ContainsWithIgnoreCase(@namespace));
    }
}