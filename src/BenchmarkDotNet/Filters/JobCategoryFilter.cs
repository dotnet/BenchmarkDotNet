using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Filters
{
    /// <summary>
    /// Filter benchmarks which belong to a job that has any of the target job categories.
    /// <remarks>
    /// Like every other category filter, this one is inclusion only: a benchmark whose job has no categories at all
    /// belongs to none of the target categories, so it is filtered out. In a config that mixes categorized and
    /// uncategorized jobs, selecting a category therefore also removes the uncategorized jobs.
    /// </remarks>
    /// </summary>
    public class JobCategoryFilter : IFilter
    {
        private readonly string[] targetCategories;

        public JobCategoryFilter(string[] targetCategories) => this.targetCategories = targetCategories;

        public bool Predicate(BenchmarkCase benchmarkCase) => targetCategories.Any(category => benchmarkCase.Job.Meta.HasCategory(category));
    }
}
