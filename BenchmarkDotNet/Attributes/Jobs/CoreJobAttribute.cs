using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class CoreJobAttribute : JobConfigBaseAttribute
    {
        public CoreJobAttribute() : base(Job.Core)
        {
        }
    }
}