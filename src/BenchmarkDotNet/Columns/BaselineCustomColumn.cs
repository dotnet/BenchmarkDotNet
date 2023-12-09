using System.Collections.Generic;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public abstract class BaselineCustomColumn : IColumn
    {
        public abstract string Id { get; }
        public abstract string ColumnName { get; }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var baseline = summary.GetBaseline(logicalGroupKey);
            bool isBaseline = summary.IsBaseline(benchmarkCase);

            if (ResultsAreInvalid(summary, benchmarkCase, baseline))
                return MetricColumn.UnknownRepresentation;

            var baselineStat = summary[baseline].ResultStatistics;
            var baselineMetrics = summary[baseline].Metrics;
            var currentStat = summary[benchmarkCase].ResultStatistics;
            var currentMetrics = summary[benchmarkCase].Metrics;

            return GetValue(summary, benchmarkCase, baselineStat, baselineMetrics, currentStat, currentMetrics, isBaseline);
        }

        [PublicAPI]
        public abstract string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline);

        public bool IsAvailable(Summary summary) => summary.HasBaselines();
        public bool AlwaysShow => true;
        public virtual ColumnCategory Category => ColumnCategory.Baseline;
        public abstract int PriorityInCategory { get; }
        public abstract bool IsNumeric { get; }
        public abstract UnitType UnitType { get; }
        public abstract string Legend { get; }
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public override string ToString() => ColumnName;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        internal static bool ResultsAreInvalid(Summary summary, BenchmarkCase benchmarkCase, BenchmarkCase? baseline)
        {
            return baseline == null ||
                   summary[baseline] == null ||
                   summary[baseline].ResultStatistics == null ||
                   !summary[baseline].ResultStatistics.CanBeInverted() ||
                   summary[benchmarkCase] == null ||
                   summary[benchmarkCase].ResultStatistics == null;
        }
    }
}