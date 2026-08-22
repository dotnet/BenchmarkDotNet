using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Adds the given categories to every job defined for the given class or assembly.
    /// It's the job equivalent of <see cref="BenchmarkCategoryAttribute"/>.
    /// <remarks>the categories of the jobs which are defined in code are preserved, the categories are added to them</remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class JobCategoryAttribute : JobConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI] protected JobCategoryAttribute() { }

        public JobCategoryAttribute(params string[] categories) : base(CreateMutatorJob(categories)) { }

        // it's a mutator job so that the categories are applied to all the jobs of the config,
        // no matter whether they were defined in code or via other attributes
        private static Job CreateMutatorJob(string[] categories) => new Job().WithCategories(categories).AsMutator().Freeze();
    }
}
