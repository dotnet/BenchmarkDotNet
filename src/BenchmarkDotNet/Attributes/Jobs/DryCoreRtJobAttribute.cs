using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class DryCoreRtJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreRtJobAttribute() : base(Job.DryCoreRT) { }
    }
}