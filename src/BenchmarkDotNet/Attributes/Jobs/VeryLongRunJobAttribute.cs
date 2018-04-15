using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class VeryLongRunJobAttribute : JobConfigBaseAttribute
    {
        public VeryLongRunJobAttribute() : base(Job.VeryLongRun)
        {
        }
    }
}