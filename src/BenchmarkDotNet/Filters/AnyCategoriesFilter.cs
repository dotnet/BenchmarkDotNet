using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// Filter benchmarks which have any of the target categories
    /// </summary>
    public class AnyCategoriesFilter : IFilter
    {
        private readonly string[] targetCategories;

        public AnyCategoriesFilter(string[] targetCategories)
        {
            this.targetCategories = targetCategories;
        }

        public bool Predicate(Benchmark benchmark) => targetCategories.Any(c => benchmark.Target.HasCategory(c));
    }
}