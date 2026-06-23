using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <inheritdoc cref="AccuracyMode.EvaluateOverhead"/>
    [PublicAPI]
    public class EvaluateOverheadAttribute : JobMutatorConfigBaseAttribute
    {
        public EvaluateOverheadAttribute(bool value = true) : base(Job.Default.WithEvaluateOverhead(value))
        {
        }
    }
}