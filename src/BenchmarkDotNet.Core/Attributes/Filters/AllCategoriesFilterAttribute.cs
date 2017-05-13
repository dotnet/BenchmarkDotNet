using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    public class AllCategoriesFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constuctor without an array in the argument list
        public AllCategoriesFilterAttribute() { }
        
        public AllCategoriesFilterAttribute(params string[] targetCategories) : base(new AllCategoriesFilter(targetCategories)) { }
    }
}