using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    public class AllCategoriesFilterAttribute : FilterConfigBaseAttribute
    {
        public AllCategoriesFilterAttribute(params string[] targetCategories) : base(new AllCategoriesFilter(targetCategories)) { }
    }
}