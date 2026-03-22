using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Specifies if the overhead should be evaluated (Idle runs) and it's average value subtracted from every result.
    /// True by default, very important for nano-benchmarks.
    /// </summary>
    [PublicAPI]
    public class EvaluateOverheadAttribute : JobMutatorConfigBaseAttribute
    {
        public EvaluateOverheadAttribute(bool value = true) : base(Job.Default.WithEvaluateOverhead(value))
        {
        }
    }
}