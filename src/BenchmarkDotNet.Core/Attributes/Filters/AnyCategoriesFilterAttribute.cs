using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    public class AnyCategoriesFilterAttribute : FilterConfigBaseAttribute
    {
        public AnyCategoriesFilterAttribute(params string[] targetCategories) : base(new AnyCategoriesFilter(targetCategories)) { }
    }
}