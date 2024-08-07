using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// <see cref="Running.BenchmarkCase"/> standard deviation constraints
    /// </summary>
    public sealed class StdDevConstraintAttribute : BenchmarkCaseConstraintAttribute
    {
        /// <summary>
        /// Instance new <see cref="StdDevConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public StdDevConstraintAttribute(ComparisonOperator @operator, double from, double? to)
        {
            Operator = @operator;
            From = from;
            To = to;
        }

        /// <summary>
        /// Instance new <see cref="StdDevConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        public StdDevConstraintAttribute(ComparisonOperator @operator, double from) :
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
            var stdDev = statistics.StandardDeviation;
            switch (Operator)
            {
                case ComparisonOperator.Equal when stdDev != From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is not equal to expected value {From}");
                    break;
                case ComparisonOperator.NotEqual when stdDev == From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is equal to expected value {From}");
                    break;
                case ComparisonOperator.Less when stdDev >= From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is greater or equal that expected value {From}");
                    break;
                case ComparisonOperator.LessOrEqual when stdDev > From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is greater  that expected value {From}");
                    break;
                case ComparisonOperator.Greater when stdDev <= From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is less or equal that expected value {From}");
                    break;
                case ComparisonOperator.GreaterOrEqual when stdDev < From:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is lest that expected value {From}");
                    break;
                case ComparisonOperator.Between when stdDev < From && stdDev > To:
                    builder.AppendLine($"{nameof(statistics.StandardDeviation)} is not between expected value [{From}-{To}]");
                    break;
                default:
                    break;
            }
        }
    }
}
