using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class RyuJitX64JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX64JobAttribute() : base(Job.RyuJitX64)
        {
        }
    }
}