using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class LegacyJitX86JobAttribute : JobConfigBaseAttribute
    {
        public LegacyJitX86JobAttribute() : base(Job.LegacyJitX86)
        {
        }
    }
}