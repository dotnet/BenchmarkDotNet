using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class MediumRunJobAttribute : JobProviderAttribute
    {
        public MediumRunJobAttribute() : base(Job.MediumRun)
        {
        }
    }
}