using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class LongRunJobAttribute : JobConfigBaseAttribute
    {
        public LongRunJobAttribute() : base(Job.LongRun)
        {
        }
    }
}