using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class CoreRtJobAttribute : JobConfigBaseAttribute
    {
        public CoreRtJobAttribute(bool baseline = false) : base(Job.CoreRT.WithBaseline(baseline))
        {
        }
    }
}