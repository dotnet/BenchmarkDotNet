using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes.Jobs
{
    public class LegacyJitX64JobAttribute : JobConfigBaseAttribute
    {
        public LegacyJitX64JobAttribute() : base(Job.LegacyJitX64)
        {
        }
    }
}