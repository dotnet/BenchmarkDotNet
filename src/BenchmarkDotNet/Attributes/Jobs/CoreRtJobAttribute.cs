using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class CoreRtJobAttribute : JobConfigBaseAttribute
    {
        public CoreRtJobAttribute(bool isBaseline = false) : base(Job.CoreRT.WithIsBaseline(isBaseline))
        {
        }
    }
}