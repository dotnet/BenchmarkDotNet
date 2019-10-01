using BenchmarkDotNet.Filters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    public class AnyCategoriesFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        public AnyCategoriesFilterAttribute() { }

        public AnyCategoriesFilterAttribute(params string[] targetCategories) : base(new AnyCategoriesFilter(targetCategories)) { }
    }
}