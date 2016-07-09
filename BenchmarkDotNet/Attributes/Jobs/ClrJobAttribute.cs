using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class ClrJobAttribute : JobConfigBaseAttribute
    {
        public ClrJobAttribute() : base(Job.Clr)
        {
        }
    }
}