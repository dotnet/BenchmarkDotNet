using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Invocation count in a single iteration.
    /// If specified, <see cref="RunMode.IterationTime"/> will be ignored.
    /// If specified, it must be a multiple of <see cref="RunMode.UnrollFactor"/>.
    /// </summary>
    public class InvocationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public InvocationCountAttribute(int invocationCount, int unrollFactor = 1)
            : base(Job.Default
                .WithInvocationCount(invocationCount)
                .WithUnrollFactor(unrollFactor))
        {
        }
    }
}