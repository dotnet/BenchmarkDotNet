using System;
using System.Collections.Generic;
using System.Linq;
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

        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            var ratio = GetRatioStatistics(current, baseline);
            if (ratio == null)
                return "NA";

            var cultureInfo = summary.GetCultureInfo();
            switch (Metric)
            {
                case RatioMetric.Mean:
                    return IsNonBaselinesPrecise(summary, baseline, benchmarkCase) ? ratio.Mean.ToString("N3", cultureInfo) : ratio.Mean.ToString("N2", cultureInfo);
                case RatioMetric.StdDev:
                    return ratio.StandardDeviation.ToString("N2", cultureInfo);
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