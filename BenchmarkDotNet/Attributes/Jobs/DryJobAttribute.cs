using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class DryJobAttribute : JobConfigBaseAttribute
    {
        public DryJobAttribute() : base(Job.Dry)
        {
        }
    }
}