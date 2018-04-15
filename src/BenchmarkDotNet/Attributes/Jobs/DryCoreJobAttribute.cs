using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class DryCoreJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreJobAttribute() : base(Job.DryCore) { }
    }
}