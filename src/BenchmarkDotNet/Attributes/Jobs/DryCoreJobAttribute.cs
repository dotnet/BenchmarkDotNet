using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class DryCoreJobAttribute : JobConfigBaseAttribute
    {
        public DryCoreJobAttribute() : base(Job.DryCore) { }
    }
}