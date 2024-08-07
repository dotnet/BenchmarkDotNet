using BenchmarkDotNet.Reports;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.Annotations
{
    /// <summary>
    /// <see cref="Running.BenchmarkCase"/> GC Gen Collections constraints
    /// </summary>
    public sealed class GCGenCollectionsConstraintAttribute : BenchmarkCaseConstraintAttribute
    {
        /// <summary>
        /// Instance new <see cref="GCGenCollectionsConstraintAttribute"/>
        /// </summary>
        /// <param name="generation"></param>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public GCGenCollectionsConstraintAttribute(GCGeneration generation, ComparisonOperator @operator, int from, int? to)
        {
            Operator = @operator;
            From = from;
            To = to;
            Generation = generation;
        }

        /// <summary>
        /// Instance new <see cref="GCGenCollectionsConstraintAttribute"/>
        /// </summary>
        /// <param name="generation"></param>
        /// <param name="operator"></param>
        /// <param name="from"></param>
        public GCGenCollectionsConstraintAttribute(GCGeneration generation, ComparisonOperator @operator, int from)
            : this(generation, @operator, from, null)
        {
        }

        /// <summary>
        /// GC Generation
        /// </summary>
        public GCGeneration Generation { get; }

        /// <summary>
        /// Comparison Operator
        /// </summary>
        public ComparisonOperator Operator { get; }
        /// <summary>
        /// From mean value
        /// </summary>
        public int From { get; }
        /// <summary>
        /// To mean value
        /// </summary>
        public int? To { get; }

        /// inheritdoc
        protected internal override void Validate(BenchmarkReport report, StringBuilder builder)
        {
            var resultRuns = report.GetResultRuns();
            var gcStats = report.GcStats;
            var genCollections = Generation switch
            {
                GCGeneration.Gen0 => gcStats.Gen0Collections,
                GCGeneration.Gen1 => gcStats.Gen1Collections,
                GCGeneration.Gen2 => gcStats.Gen2Collections,
                _ => throw new System.NotSupportedException(),
            };
            switch (Operator)
            {
                case ComparisonOperator.Equal when genCollections != From:
                    builder.AppendLine($"{Generation} is not equal to expected value {From}");
                    break;
                case ComparisonOperator.NotEqual when genCollections == From:
                    builder.AppendLine($"{Generation} is equal to expected value {From}");
                    break;
                case ComparisonOperator.Less when genCollections >= From:
                    builder.AppendLine($"{Generation} is greater or equal that expected value {From}");
                    break;
                case ComparisonOperator.LessOrEqual when genCollections > From:
                    builder.AppendLine($"{Generation} is greater  that expected value {From}");
                    break;
                case ComparisonOperator.Greater when genCollections <= From:
                    builder.AppendLine($"{Generation} is less or equal that expected value {From}");
                    break;
                case ComparisonOperator.GreaterOrEqual when genCollections < From:
                    builder.AppendLine($"{Generation} is lest that expected value {From}");
                    break;
                case ComparisonOperator.Between when genCollections < From && genCollections > To:
                    builder.AppendLine($"{Generation} is not betwenn  expected value [{From}-{To}]");
                    break;
                default:
                    break;
            }
        }
    }
}
