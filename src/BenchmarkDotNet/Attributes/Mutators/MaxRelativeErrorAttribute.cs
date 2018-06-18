using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Maximum acceptable error for a benchmark (by default, BenchmarkDotNet continue iterations until the actual error is less than the specified error).
    /// The default value is 0.02.
    /// <remarks>If <see cref="AccuracyMode.MaxAbsoluteError"/> is also provided, the smallest value is used as stop criteria.</remarks>
    /// </summary>
    public class MaxRelativeErrorAttribute : JobMutatorConfigBaseAttribute
    {
        public MaxRelativeErrorAttribute(double maxRelativeError) : base(Job.Default.WithMaxRelativeError(maxRelativeError))
        {
        }
    }
}