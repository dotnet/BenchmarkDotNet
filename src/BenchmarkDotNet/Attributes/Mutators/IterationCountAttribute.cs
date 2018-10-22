using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// How many target iterations should be performed
    /// If specified, <see cref="RunMode.MinIterationCount"/> will be ignored.
    /// If specified, <see cref="RunMode.MaxIterationCount"/> will be ignored.
    /// </summary>
    [PublicAPI]
    public class IterationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public IterationCountAttribute(int targetIterationCount) : base(Job.Default.WithIterationCount(targetIterationCount))
        {
        }
    }
}