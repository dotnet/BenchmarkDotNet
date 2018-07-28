using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class RyuJitX86JobAttribute : JobConfigBaseAttribute
    {
        public RyuJitX86JobAttribute() : base(Job.RyuJitX86)
        {
        }
    }
}