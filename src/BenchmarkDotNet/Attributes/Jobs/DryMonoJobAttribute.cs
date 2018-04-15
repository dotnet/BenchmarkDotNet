using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class DryMonoJobAttribute : JobConfigBaseAttribute
    {
        public DryMonoJobAttribute() : base(Job.DryMono) { }
    }
}