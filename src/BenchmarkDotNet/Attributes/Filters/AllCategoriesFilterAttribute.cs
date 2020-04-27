using BenchmarkDotNet.Filters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class AllCategoriesFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        public AllCategoriesFilterAttribute() { }

        public AllCategoriesFilterAttribute(params string[] targetCategories) : base(new AllCategoriesFilter(targetCategories)) { }
    }
}