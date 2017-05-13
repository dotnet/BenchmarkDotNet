using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    public class CategoryFilterAttribute : FilterConfigBaseAttribute
    {
        public CategoryFilterAttribute(string targetCategory) : base(new CategoryFilter(targetCategory)) { }
    }
}