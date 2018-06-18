using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
    /// Doesn't have a default value.
    /// <remarks>If <see cref="AccuracyMode.MaxRelativeError"/> is also provided, the smallest value is used as stop criteria.</remarks>
    /// </summary>
    public class MaxAbsoluteErrorAttribute : JobMutatorConfigBaseAttribute
    {
        public MaxAbsoluteErrorAttribute(double nanoseconds) : base(Job.Default.WithMaxAbsoluteError(TimeInterval.FromNanoseconds(nanoseconds)))
        {
        }
    }
}