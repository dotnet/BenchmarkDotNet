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

        public bool Predicate(BenchmarkCase benchmarkCase) => namespaces.Any(@namespace => benchmarkCase.Target.Type.Namespace.ContainsWithIgnoreCase(@namespace));
    }
}