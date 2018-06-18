using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Minimum count of benchmark invocations per iteration.
    /// The default value is 4.
    /// </summary>
    public class MinInvokeCountAttribute : JobMutatorConfigBaseAttribute
    {
        public MinInvokeCountAttribute(int minInvokeCount) : base(Job.Default.WithMinInvokeCount(minInvokeCount))
        {
        }
    }
}