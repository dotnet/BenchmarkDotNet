using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class RyuJitX86JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX86JobAttribute() : base(Job.RyuJitX86)
        {
        }
    }
}