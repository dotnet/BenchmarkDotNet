using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class DryMonoJobAttribute : JobConfigBaseAttribute
    {
        public DryMonoJobAttribute() : base(Job.DryMono) { }
    }
}