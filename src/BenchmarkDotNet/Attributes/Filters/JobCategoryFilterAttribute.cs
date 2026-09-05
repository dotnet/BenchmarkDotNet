using BenchmarkDotNet.Filters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Runs only the benchmarks whose job belongs to any of the given job categories,
    /// see <see cref="JobCategoryFilter"/>.
    /// </summary>
    public class JobCategoryFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        public JobCategoryFilterAttribute() { }

        public JobCategoryFilterAttribute(params string[] targetCategories) : base(new JobCategoryFilter(targetCategories)) { }
    }
}
