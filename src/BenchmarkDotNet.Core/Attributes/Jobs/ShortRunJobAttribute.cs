using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class ShortRunJobAttribute: JobConfigBaseAttribute
    {
        public ShortRunJobAttribute() : base(Job.ShortRun)
        {
        }
    }
}