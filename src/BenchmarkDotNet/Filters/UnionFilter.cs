using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    public class UnionFilter : IFilter
    {
        private readonly IFilter[] filters;

        public UnionFilter(params IFilter[] filters) => this.filters = filters;

        public bool Predicate(BenchmarkCase benchmarkCase) => filters.All(filter => filter.Predicate(benchmarkCase));
    }
}