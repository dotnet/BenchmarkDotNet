using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class RyuJitX64JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX64JobAttribute() : base(Job.RyuJitX64)
        {
        }
    }
}