using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// Filter benchmarks which have all the target categories
    /// </summary>
    public class AllCategoriesFilter : IFilter
    {
        private readonly string[] targetCategories;

        public AllCategoriesFilter(string[] targetCategories)
        {
            this.targetCategories = targetCategories;
        }

        public bool Predicate(Benchmark benchmark) => targetCategories.All(c => benchmark.Target.HasCategory(c));
    }
}