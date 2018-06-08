using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class CoreJobAttribute : JobConfigBaseAttribute
    {
        public CoreJobAttribute(bool isBaseline = false) : base(Job.Core.WithIsBaseline(isBaseline))
        {
        }
    }
}