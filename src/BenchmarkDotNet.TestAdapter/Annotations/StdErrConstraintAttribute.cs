using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// <see cref="Running.BenchmarkCase"/> standard error constraints
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class StdErrConstraintAttribute : BenchmarkCaseConstraintAttribute
    {
        /// <summary>
        /// Instance new <see cref="StdErrConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public StdErrConstraintAttribute(ComparisonOperator @operator, double from, double? to)
        {
            Operator = @operator;
            From = from;
            To = to;
        }

        /// <summary>
        /// Instance new <see cref="StdErrConstraintAttribute"/>
        /// </summary>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        public StdErrConstraintAttribute(ComparisonOperator @operator, double from) :
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
            var stdErr = statistics.StandardError;
            switch (Operator)
            {
                case ComparisonOperator.Equal when stdErr != From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is not equal to expected value {From}");
                    break;
                case ComparisonOperator.NotEqual when stdErr == From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is equal to expected value {From}");
                    break;
                case ComparisonOperator.Less when stdErr >= From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is greater or equal that expected value {From}");
                    break;
                case ComparisonOperator.LessOrEqual when stdErr > From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is greater  that expected value {From}");
                    break;
                case ComparisonOperator.Greater when stdErr <= From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is less or equal that expected value {From}");
                    break;
                case ComparisonOperator.GreaterOrEqual when stdErr < From:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is lest that expected value {From}");
                    break;
                case ComparisonOperator.Between when stdErr < From && stdErr > To:
                    builder.AppendLine($"{nameof(statistics.StandardError)} is not between expected value [{From}-{To}]");
                    break;
                default:
                    break;
            }

        }
    }
}
