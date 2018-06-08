using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class RyuJitX86JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX86JobAttribute() : base(Job.RyuJitX86)
        {
        }
    }
}