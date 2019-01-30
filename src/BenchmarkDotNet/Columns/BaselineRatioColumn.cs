using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public class BaselineRatioColumn : BaselineCustomColumn
    {
        public enum RatioMetric
        {
            Mean,
            StdDev
        }

        public static readonly IColumn RatioMean = new BaselineRatioColumn(RatioMetric.Mean);
        public static readonly IColumn RatioStdDev = new BaselineRatioColumn(RatioMetric.StdDev);

        public RatioMetric Metric { get; }

        private BaselineRatioColumn(RatioMetric metric)
        {
            Metric = metric;
        }

        public override string Id => nameof(BaselineRatioColumn) + "." + Metric;

        public override string ColumnName
        {
            get
            {
                switch (Metric)
                {
                    case RatioMetric.Mean:
                        return "Ratio";
                    case RatioMetric.StdDev:
                        return "RatioSD";
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        internal override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, Statistics current, bool isBaseline)
        {
            var ratio = GetRatioStatistics(current, baseline);
            if (ratio == null)
                return "NA";

            switch (Metric)
            {
                case RatioMetric.Mean:
                    return IsNonBaselinesPrecise(summary, baseline, benchmarkCase) ? ratio.Mean.ToStr("N3") : ratio.Mean.ToStr("N2");
                case RatioMetric.StdDev:
                    return ratio.StandardDeviation.ToStr("N2");
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);
            return nonBaselines.Any(x => GetRatioStatistics(summary[x].ResultStatistics, baselineStat)?.Mean < 0.01);
        }

        [CanBeNull]
        private static Statistics GetRatioStatistics([CanBeNull] Statistics current, [CanBeNull] Statistics baseline)
        {
            if (current == null || current.N < 1)
                return null;
            if (baseline == null || baseline.N < 1)
                return null;
            try
            {
                return Statistics.Divide(current, baseline);
            }
            catch (DivideByZeroException)
            {
                return null;
            }
        }

        public override int PriorityInCategory => (int) Metric;
        public override bool IsNumeric => true;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend
        {
            get
            {
                switch (Metric)
                {
                    case RatioMetric.Mean:
                        return "Mean of the ratio distribution ([Current]/[Baseline])";
                    case RatioMetric.StdDev:
                        return "Standard deviation of the ratio distribution ([Current]/[Baseline])";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Metric));
                }
            }
        }
    }
}