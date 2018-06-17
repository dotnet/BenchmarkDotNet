using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// How many target iterations should be performed
    /// If specified, <see cref="RunMode.MinTargetIterationCount"/> will be ignored.
    /// If specified, <see cref="RunMode.MaxTargetIterationCount"/> will be ignored.
    /// </summary>
    public class IterationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public IterationCountAttribute(int targetIterationCount) : base(Job.Default.WithTargetCount(targetIterationCount))
        {
        }
    }
}