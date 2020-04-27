using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Maximum count of warmup iterations that should be performed
    /// The default value is 50
    /// </summary>
    public class MaxWarmupCountAttribute : JobMutatorConfigBaseAttribute
    {
        /// <param name="maxWarmupCount">Maximum count of warmup iterations that should be performed. The default value is 50</param>
        /// <param name="forceAutoWarmup">if set to true, will overwrite WarmupCount of the global config</param>
        public MaxWarmupCountAttribute(int maxWarmupCount, bool forceAutoWarmup = false)
            : base(forceAutoWarmup
                ? Job.Default.WithMaxWarmupCount(maxWarmupCount).WithWarmupCount(EngineResolver.ForceAutoWarmup)
                : Job.Default.WithMaxWarmupCount(maxWarmupCount))
        {
        }
    }
}