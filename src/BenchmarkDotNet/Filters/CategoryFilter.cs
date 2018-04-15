using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// Filter benchmarks which have the target category
    /// </summary>
    public class CategoryFilter : IFilter
    {
        private readonly string targetCategory;

        public CategoryFilter(string targetCategory)
        {
            this.targetCategory = targetCategory;
        }

        public bool Predicate(Benchmark benchmark) => benchmark.Target.HasCategory(targetCategory);
    }
}