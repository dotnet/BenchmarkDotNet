using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class LongRunJobAttribute : JobProviderAttribute
    {
        public LongRunJobAttribute() : base(Job.LongRun)
        {
        }
    }
}