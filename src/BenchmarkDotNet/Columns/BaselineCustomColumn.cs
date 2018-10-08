using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

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
            bool invalidResults = baseline == null ||
                                 summary[baseline] == null ||
                                 summary[baseline].ResultStatistics == null ||
                                 !summary[baseline].ResultStatistics.CanBeInverted() ||
                                 summary[benchmarkCase] == null ||
                                 summary[benchmarkCase].ResultStatistics == null;

            if (invalidResults)
                return "?";

            var baselineStat = summary[baseline].ResultStatistics;
            var currentStat = summary[benchmarkCase].ResultStatistics;

            return GetValue(summary, benchmarkCase, baselineStat, currentStat, isBaseline);
        }

        protected abstract string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, Statistics current, bool isBaseline);

        public bool IsAvailable(Summary summary) => summary.HasBaselines();
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Baseline;
        public abstract int PriorityInCategory { get; }
        public abstract bool IsNumeric { get; }
        public abstract UnitType UnitType { get; }
        public abstract string Legend { get; }
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);
        public override string ToString() => ColumnName;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    }
}