using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class CoreRtJobAttribute : JobConfigBaseAttribute
    {
        public CoreRtJobAttribute(bool isBaseline = false) : base(Job.CoreRT.WithIsBaseline(isBaseline))
        {
        }
    }
}