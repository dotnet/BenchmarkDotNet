using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class DryJobAttribute : JobConfigBaseAttribute
    {
        public DryJobAttribute() : base(Job.Dry)
        {
        }
    }
}