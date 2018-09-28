using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineScaledColumn : BaselineCustomColumn
    {
        public enum ScaledKind
        {
            Mean,
            StdDev
        }

        public static readonly IColumn Scaled = new BaselineScaledColumn(ScaledKind.Mean);
        public static readonly IColumn ScaledStdDev = new BaselineScaledColumn(ScaledKind.StdDev);

        public ScaledKind Kind { get; }

        private BaselineScaledColumn(ScaledKind kind)
        {
            Kind = kind;
        }

        public override string Id => nameof(BaselineScaledColumn) + "." + Kind;

        public override string ColumnName
        {
            get
            {
                switch (Kind)
                {
                    case ScaledKind.Mean:
                        return "Scaled";
                    case ScaledKind.StdDev:
                        return "ScaledSD";
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        protected override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, Statistics current, bool isBaseline)
        {
            double mean = isBaseline ? 1 : Statistics.DivMean(current, baseline);
            double stdDev = isBaseline ? 0 : Math.Sqrt(Statistics.DivVariance(current, baseline));

            switch (Kind)
            {
                case ScaledKind.Mean:
                    return IsNonBaselinesPrecise(summary, baseline, benchmarkCase) ? mean.ToStr("N3") : mean.ToStr("N2");
                case ScaledKind.StdDev:
                    return stdDev.ToStr("N2");
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);

            return nonBaselines.Any(x => Statistics.DivMean(summary[x].ResultStatistics, baselineStat) < 0.01);
        }

        public override int PriorityInCategory => (int) Kind;
        public override bool IsNumeric => true;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend
        {
            get
            {
                switch (Kind)
                {
                    case ScaledKind.Mean:
                        return "Mean(CurrentBenchmark) / Mean(BaselineBenchmark)";
                    case ScaledKind.StdDev:
                        return "Standard deviation of ratio of distribution of [CurrentBenchmark] and [BaselineBenchmark]";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Kind));
                }
            }
        }
    }
}