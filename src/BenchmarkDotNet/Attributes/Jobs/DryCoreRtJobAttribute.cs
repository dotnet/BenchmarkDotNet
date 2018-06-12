using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class DryCoreRtJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreRtJobAttribute() : base(Job.DryCoreRT) { }
    }
}