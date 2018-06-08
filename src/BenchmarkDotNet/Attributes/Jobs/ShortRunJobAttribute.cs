using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class ShortRunJobAttribute: JobConfigBaseAttribute
    {
        public ShortRunJobAttribute() : base(Job.ShortRun)
        {
        }
    }
}