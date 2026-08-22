using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// Filter benchmarks which belong to a job that has any of the target job categories
    /// </summary>
    public class JobCategoryFilter : IFilter
    {
        private readonly string[] targetCategories;

        public JobCategoryFilter(string[] targetCategories) => this.targetCategories = targetCategories;

        public bool Predicate(BenchmarkCase benchmarkCase) => targetCategories.Any(category => benchmarkCase.Job.Meta.HasCategory(category));
    }
}
