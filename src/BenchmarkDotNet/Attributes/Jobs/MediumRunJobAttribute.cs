using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class MediumRunJobAttribute : JobConfigBaseAttribute
    {
        public MediumRunJobAttribute() : base(Job.MediumRun)
        {
        }
    }
}