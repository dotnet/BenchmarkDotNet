using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Minimum count of target iterations that should be performed.
    /// The default value is 15.
    /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/> is not going to work.</remarks>
    /// </summary>
    [PublicAPI]
    public class MinIterationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public MinIterationCountAttribute(int minTargetIterationCount) : base(Job.Default.WithMinIterationCount(minTargetIterationCount))
        {
        }
    }
}