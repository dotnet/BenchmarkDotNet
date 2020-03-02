using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// How many warmup iterations should be performed.
    /// </summary>
    [PublicAPI]
    public class WarmupCountAttribute : JobMutatorConfigBaseAttribute
    {
        public WarmupCountAttribute(int warmupCount) : base(Job.Default.WithWarmupCount(warmupCount))
        {
        }
    }
}