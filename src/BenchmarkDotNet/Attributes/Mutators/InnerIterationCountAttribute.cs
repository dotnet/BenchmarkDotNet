using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Invocation count in a single iteration.
    /// Does exactly the same as InvocationCountAttribute, added to make porting from xunit-performance to BenchmarkDotNet easier
    /// </summary>
    public class InnerIterationCountAttribute : JobMutatorConfigBaseAttribute
    {
        public InnerIterationCountAttribute(int invocationCount) 
            : base(Job.Default
                .WithInvocationCount(invocationCount)
                .WithUnrollFactor(1)) // it's for xunit-performance porting purpose, where the idea of unroll factor did not exist
        {
        }
    }
}