using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class LongRunJobAttribute : JobConfigBaseAttribute
    {
        public LongRunJobAttribute() : base(Job.LongRun)
        {
        }
    }
}