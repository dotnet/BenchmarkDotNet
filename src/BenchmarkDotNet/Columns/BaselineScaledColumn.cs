using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    [Obsolete("Use BaselineRatioColumn"), PublicAPI]    
    public class BaselineScaledColumn : BaselineCustomColumn
    {
        public enum ScaledKind
        {
            Mean
        }

        public static readonly IColumn Scaled = new BaselineScaledColumn(ScaledKind.Mean);

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
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        protected override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, Statistics current, bool isBaseline)
        {
            double mean = isBaseline ? 1 : Statistics.DivMean(current, baseline);

            switch (Kind)
            {
                case ScaledKind.Mean:
                    return IsNonBaselinesPrecise(summary, baseline, benchmarkCase) ? mean.ToStr("N3") : mean.ToStr("N2");
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
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Kind));
                }
            }
        }
    }
}