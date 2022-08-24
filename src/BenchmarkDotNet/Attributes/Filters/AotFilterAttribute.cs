using BenchmarkDotNet.Filters;

namespace BenchmarkDotNet.Attributes.Filters
{
    public class AotFilterAttribute : FilterConfigBaseAttribute
    {
        public AotFilterAttribute(string reason = null)
            : base(new SimpleFilter(benchmark => !benchmark.GetRuntime().IsAOT))
        {
        }
    }
}
