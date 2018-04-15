using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class DisjunctionFilter : IFilter
    {
        private readonly IFilter[] filters;

        public DisjunctionFilter(params IFilter[] filters)
        {
            this.filters = filters;
        }

        public bool Predicate(Benchmark benchmark) => filters.Any(filter => filter.Predicate(benchmark));
    }
}