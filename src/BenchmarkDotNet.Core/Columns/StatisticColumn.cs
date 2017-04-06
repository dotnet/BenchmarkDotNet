using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class StatisticColumn : IColumn
    {
        public enum Priority
        {
            Main, Quartile, Percentiles, Additional
        }

        public static readonly IColumn Mean = new StatisticColumn("Mean", s => s.Mean, Priority.Main);
        public static readonly IColumn StdErr = new StatisticColumn("StdErr", s => s.StandardError, Priority.Main);
        public static readonly IColumn StdDev = new StatisticColumn("StdDev", s => s.StandardDeviation, Priority.Main);

        public static readonly IColumn OperationsPerSecond = new StatisticColumn("Op/s", s => 1.0 * 1000 * 1000 * 1000 / s.Mean, Priority.Additional, false);

        public static readonly IColumn Min = new StatisticColumn("Min", s => s.Min, Priority.Quartile);
        public static readonly IColumn Q1 = new StatisticColumn("Q1", s => s.Q1, Priority.Quartile);
        public static readonly IColumn Median = new StatisticColumn("Median", s => s.Median, Priority.Quartile);
        public static readonly IColumn Q3 = new StatisticColumn("Q3", s => s.Q3, Priority.Quartile);
        public static readonly IColumn Max = new StatisticColumn("Max", s => s.Max, Priority.Quartile);

        public static readonly IColumn Skewness = new StatisticColumn("Skewness", s => s.Skewness, Priority.Additional ,false);
        public static readonly IColumn Kurtosis = new StatisticColumn("Kurtosis", s => s.Kurtosis, Priority.Additional, false);

        public static readonly IColumn P0 = new StatisticColumn("P0", s => s.Percentiles.P0, Priority.Percentiles);
        public static readonly IColumn P25 = new StatisticColumn("P25", s => s.Percentiles.P25, Priority.Percentiles);
        public static readonly IColumn P50 = new StatisticColumn("P50", s => s.Percentiles.P50, Priority.Percentiles);
        public static readonly IColumn P67 = new StatisticColumn("P67", s => s.Percentiles.P67, Priority.Percentiles);
        public static readonly IColumn P80 = new StatisticColumn("P80", s => s.Percentiles.P80, Priority.Percentiles);
        public static readonly IColumn P85 = new StatisticColumn("P85", s => s.Percentiles.P85, Priority.Percentiles);
        public static readonly IColumn P90 = new StatisticColumn("P90", s => s.Percentiles.P90, Priority.Percentiles);
        public static readonly IColumn P95 = new StatisticColumn("P95", s => s.Percentiles.P95, Priority.Percentiles);
        public static readonly IColumn P100 = new StatisticColumn("P100", s => s.Percentiles.P100, Priority.Percentiles);

        public static IColumn CiLower(ConfidenceLevel level) => new StatisticColumn($"CI{level.ToPercentStr()} Lower",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Lower, Priority.Additional);

        public static IColumn CiUpper(ConfidenceLevel level) => new StatisticColumn($"CI{level.ToPercentStr()} Upper",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Upper, Priority.Additional);

        public static IColumn CiError(ConfidenceLevel level) => new StatisticColumn($"CI{level.ToPercentStr()} Error",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Margin, Priority.Additional);

        public static readonly IColumn[] AllStatistics = { Mean, StdErr, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };

        private readonly Func<Statistics, double> calc;
        private readonly bool isTimeColumn;
        public string Id => nameof(StatisticColumn) + "." + ColumnName;
        public string ColumnName { get; }
        private readonly Priority priority;

        private StatisticColumn(string columnName, Func<Statistics, double> calc, Priority priority, bool isTimeColumn = true)
        {
            this.calc = calc;
            this.priority = priority;
            this.isTimeColumn = isTimeColumn;
            ColumnName = columnName;
        }

        public string GetValue(Summary summary, Benchmark benchmark) =>
            Format(summary[benchmark].ResultStatistics, summary.TimeUnit);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public int PriorityInCategory => (int) priority;

        private string Format(Statistics statistics, TimeUnit timeUnit)
        {
            if (statistics == null)
                return "NA";
            double value = calc(statistics);
            return isTimeColumn ? value.ToTimeStr(timeUnit) : value.ToStr();
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    }
}