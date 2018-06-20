using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class CoreJobAttribute : JobConfigBaseAttribute
    {
        public CoreJobAttribute(bool baseline = false) : base(Job.Core.WithBaseline(baseline))
        {
        }
    }
}