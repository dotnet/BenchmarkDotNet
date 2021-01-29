using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Minimum time of a single iteration. Unlike Run.IterationTime, this characteristic specifies only the lower limit. In case of need, BenchmarkDotNet can increase this value.
    /// The default value is 500 milliseconds.
    /// </summary>
    [PublicAPI]
    public class MinIterationTimeAttribute : JobMutatorConfigBaseAttribute
    {
        public MinIterationTimeAttribute(double milliseconds) : base(Job.Default.WithMinIterationTime(TimeInterval.FromMilliseconds(milliseconds)))
        {
        }
    }
}