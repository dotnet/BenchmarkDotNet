using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    public class LegacyJitX64JobAttribute : JobConfigBaseAttribute
    {
        public LegacyJitX64JobAttribute() : base(Job.LegacyJitX64)
        {
        }
    }
}