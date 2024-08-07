using BenchmarkDotNet.Reports;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// <see cref="Running.BenchmarkCase"/> GC Total Allocated bytes constraints
    /// </summary>
    public sealed class TotalAllocatedBytesConstraintAttribute : BenchmarkCaseConstraintAttribute
    {
        private readonly bool excludeAllocationQuantumSideEffects;

        /// <summary>
        /// Instance new <see cref="TotalAllocatedBytesConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="excludeAllocationQuantumSideEffects">Allocation quantum can affecting some of our nano-benchmarks in non-deterministic way.
        /// when this parameter is set to true and the number of all allocated bytes is less or equal AQ, we ignore AQ and put 0 to the results</param>
        public TotalAllocatedBytesConstraintAttribute(ComparisonOperator @operator,
            long from,
            long? to,
            bool excludeAllocationQuantumSideEffects = false
            )
        {
            Operator = @operator;
            From = from;
            To = to;
            this.excludeAllocationQuantumSideEffects = excludeAllocationQuantumSideEffects;
        }

        /// <summary>
        /// Instance new <see cref="TotalAllocatedBytesConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="excludeAllocationQuantumSideEffects">Allocation quantum can affecting some of our nano-benchmarks in non-deterministic way.
        /// when this parameter is set to true and the number of all allocated bytes is less or equal AQ, we ignore AQ and put 0 to the results</param>
        public TotalAllocatedBytesConstraintAttribute(ComparisonOperator @operator, long from, bool excludeAllocationQuantumSideEffects = false) :
            this(@operator, from, null)
        {
        }

        /// <summary>
        /// Comparison Operator
        /// </summary>
        public ComparisonOperator Operator { get; }
        /// <summary>
        /// From mean value
        /// </summary>
        public long From { get; }
        /// <summary>
        /// To mean value
        /// </summary>
        public long? To { get; }

        /// inheritdoc
        protected internal override void Validate(BenchmarkReport report, StringBuilder builder)
        {
            var totalAllocatedBytes = report.GcStats.GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects);
            switch (Operator)
            {
                case ComparisonOperator.Equal when totalAllocatedBytes != From:
                    builder.AppendLine($"GC total allocated bytes is not equal to expected value {From}");
                    break;
                case ComparisonOperator.NotEqual when totalAllocatedBytes == From:
                    builder.AppendLine($"GC total allocated bytes is equal to expected value {From}");
                    break;
                case ComparisonOperator.Less when totalAllocatedBytes >= From:
                    builder.AppendLine($"GC total allocated bytes is greater or equal that expected value {From}");
                    break;
                case ComparisonOperator.LessOrEqual when totalAllocatedBytes > From:
                    builder.AppendLine($"GC total allocated bytes is greater  that expected value {From}");
                    break;
                case ComparisonOperator.Greater when totalAllocatedBytes <= From:
                    builder.AppendLine($"GC total allocated bytes is less or equal that expected value {From}");
                    break;
                case ComparisonOperator.GreaterOrEqual when totalAllocatedBytes < From:
                    builder.AppendLine($"GC total allocated bytes is lest that expected value {From}");
                    break;
                case ComparisonOperator.Between when totalAllocatedBytes < From && totalAllocatedBytes > To:
                    builder.AppendLine($"GC total allocated bytes is not between  expected value [{From}-{To}]");
                    break;
                default:
                    break;
            }
        }
    }
}
