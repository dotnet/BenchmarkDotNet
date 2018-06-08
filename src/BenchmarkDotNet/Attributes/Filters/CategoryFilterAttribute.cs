using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes
{
    public class CategoryFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constuctor without an array in the argument list
        public CategoryFilterAttribute() { }
        
        public CategoryFilterAttribute(string targetCategory) : base(new CategoryFilter(targetCategory)) { }
    }
}