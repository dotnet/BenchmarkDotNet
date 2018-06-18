using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes 
{
    /// <summary>
    /// How many warmup iterations should be performed.
    /// </summary>
    public class WarmupCountAttribute : JobMutatorConfigBaseAttribute
    {
        public WarmupCountAttribute(int warmupCount) : base(Job.Default.WithWarmupCount(warmupCount))
        {
        }
    }
}