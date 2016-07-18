using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class MonoLlvmJobAttribute : JobConfigBaseAttribute
    {
        public MonoLlvmJobAttribute() : base(Job.MonoLlvm)
        {
        }
    }
}