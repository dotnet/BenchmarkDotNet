using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class MediumRunJobAttribute : JobConfigBaseAttribute
    {
        public MediumRunJobAttribute() : base(Job.MediumRun)
        {
        }
    }
}