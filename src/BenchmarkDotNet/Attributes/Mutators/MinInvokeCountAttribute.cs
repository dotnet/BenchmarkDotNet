using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Minimum count of benchmark invocations per iteration.
    /// The default value is 4.
    /// </summary>
    [PublicAPI]
    public class MinInvokeCountAttribute : JobMutatorConfigBaseAttribute
    {
        public MinInvokeCountAttribute(int minInvokeCount) : base(Job.Default.WithMinInvokeCount(minInvokeCount))
        {
        }
    }
}