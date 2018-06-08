using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class VeryLongRunJobAttribute : JobConfigBaseAttribute
    {
        public VeryLongRunJobAttribute() : base(Job.VeryLongRun)
        {
        }
    }
}