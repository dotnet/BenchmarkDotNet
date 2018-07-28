using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration.
    /// The default value is 500 milliseconds.
    /// </summary>
    public class IterationTimeAttribute : JobMutatorConfigBaseAttribute
    {
        public IterationTimeAttribute(double milliseconds) : base(Job.Default.WithIterationTime(TimeInterval.FromMilliseconds(milliseconds)))
        {
        }
    }
}