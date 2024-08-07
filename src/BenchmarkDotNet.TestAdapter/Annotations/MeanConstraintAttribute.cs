using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// /// <see cref="Running.BenchmarkCase"/> mean constraints
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class MeanConstraintAttribute : BenchmarkCaseConstraintAttribute
    {
        /// <summary>
        /// Instance new <see cref="MeanConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public MeanConstraintAttribute(ComparisonOperator @operator, double from, double? to)
        {
            Operator = @operator;
            From = from;
            To = to;
        }

        /// <summary>
        /// Instance new <see cref="MeanConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        public MeanConstraintAttribute(ComparisonOperator @operator, double from) :
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
        public double From { get; }
        /// <summary>
        /// To mean value
        /// </summary>
        public double? To { get; }

        /// inheritdoc
        protected internal override void Validate(BenchmarkReport report, StringBuilder builder)
        {
            var resultRuns = report.GetResultRuns();
            var statistics = resultRuns.GetStatistics();
            switch (Operator)
            {
                case ComparisonOperator.Equal when statistics.Mean != From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is not equal to expected value {From}");
                    break;
                case ComparisonOperator.NotEqual when statistics.Mean == From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is equal to expected value {From}");
                    break;
                case ComparisonOperator.Less when statistics.Mean >= From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is greater or equal that expected value {From}");
                    break;
                case ComparisonOperator.LessOrEqual when statistics.Mean > From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is greater  that expected value {From}");
                    break;
                case ComparisonOperator.Greater when statistics.Mean <= From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is less or equal that expected value {From}");
                    break;
                case ComparisonOperator.GreaterOrEqual when statistics.Mean < From:
                    builder.AppendLine($"{nameof(statistics.Mean)} is lest that expected value {From}");
                    break;
                case ComparisonOperator.Between when statistics.Mean < From && statistics.Mean > To:
                    builder.AppendLine($"{nameof(statistics.Mean)} is not between expected value [{From}-{To}]");
                    break;
                default:
                    break;
            }

        }
    }
}
