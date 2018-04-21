using System.Linq;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class NamespacesFilter : IFilter
    {
        private readonly string[] namespaces;

        public NamespacesFilter(string[] namespaces) => this.namespaces = namespaces;

        public bool Predicate(Benchmark benchmark) => namespaces.Any(@namespace => benchmark.Target.Type.Namespace.ContainsWithIgnoreCase(@namespace));
    }
}