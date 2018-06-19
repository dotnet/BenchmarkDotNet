using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Maximum count of target iterations that should be performed
    /// The default value is 100
    /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/>  is not going to work</remarks>
    /// </summary>
    public class MaxIterationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public MaxIterationCountAttribute(int maxTargetIterationCount) : base(Job.Default.WithMaxIterationCount(maxTargetIterationCount))
        {
        }
    }
}