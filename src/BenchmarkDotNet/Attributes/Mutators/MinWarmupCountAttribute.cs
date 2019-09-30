using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Minimum count of warmup iterations that should be performed
    /// The default value is 6
    /// </summary>
    public class MinWarmupCountAttribute : JobMutatorConfigBaseAttribute
    {
        /// <param name="minWarmupCount">Minimum count of warmup iterations that should be performed. The default value is 6</param>
        /// <param name="forceAutoWarmup">if set to true, will overwrite WarmupCount in the global config</param>
        public MinWarmupCountAttribute(int minWarmupCount, bool forceAutoWarmup = false)
            : base(forceAutoWarmup
                ? Job.Default.WithMinWarmupCount(minWarmupCount).WithWarmupCount(EngineResolver.ForceAutoWarmup)
                : Job.Default.WithMinWarmupCount(minWarmupCount))
        {
        }
    }
}