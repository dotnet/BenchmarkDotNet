using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class MonoJobAttribute : JobConfigBaseAttribute
    {
        public MonoJobAttribute() : base(Job.Mono)
        {
        }
    }
}