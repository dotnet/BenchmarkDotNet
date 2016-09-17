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
        public static readonly IColumn Mean = new StatisticColumn("Mean", s => s.Mean);
        public static readonly IColumn StdErr = new StatisticColumn("StdErr", s => s.StandardError);

        public static readonly IColumn StdDev = new StatisticColumn("StdDev", s => s.StandardDeviation);
        public static readonly IColumn OperationsPerSecond = new StatisticColumn("Op/s", s => 1.0 * 1000 * 1000 * 1000 / s.Mean, false);

        public static readonly IColumn Min = new StatisticColumn("Min", s => s.Min);
        public static readonly IColumn Q1 = new StatisticColumn("Q1", s => s.Q1);
        public static readonly IColumn Median = new StatisticColumn("Median", s => s.Median);
        public static readonly IColumn Q3 = new StatisticColumn("Q3", s => s.Q3);
        public static readonly IColumn Max = new StatisticColumn("Max", s => s.Max);

        public static readonly IColumn Skewness = new StatisticColumn("Skewness", s => s.Skewness, false);
        public static readonly IColumn Kurtosis = new StatisticColumn("Kurtosis", s => s.Kurtosis, false);

        public static readonly IColumn P0 = new StatisticColumn("P0", s => s.Percentiles.P0);
        public static readonly IColumn P25 = new StatisticColumn("P25", s => s.Percentiles.P25);
        public static readonly IColumn P50 = new StatisticColumn("P50", s => s.Percentiles.P50);
        public static readonly IColumn P67 = new StatisticColumn("P67", s => s.Percentiles.P67);
        public static readonly IColumn P80 = new StatisticColumn("P80", s => s.Percentiles.P80);
        public static readonly IColumn P85 = new StatisticColumn("P85", s => s.Percentiles.P85);
        public static readonly IColumn P90 = new StatisticColumn("P90", s => s.Percentiles.P90);
        public static readonly IColumn P95 = new StatisticColumn("P95", s => s.Percentiles.P95);
        public static readonly IColumn P100 = new StatisticColumn("P100", s => s.Percentiles.P100);

        public static IColumn CiLower(ConfidenceLevel level) => new StatisticColumn($"CI {level.ToPercent()}% Lower",
            s => new ConfidenceInterval(s.Mean, s.StandardError, level).Lower);
        public static IColumn CiUpper(ConfidenceLevel level) => new StatisticColumn($"CI {level.ToPercent()}% Upper",
            s => new ConfidenceInterval(s.Mean, s.StandardError, level).Upper);

        public static readonly IColumn[] AllStatistics = { Mean, StdErr, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };

        private readonly Func<Statistics, double> calc;
        private readonly bool isTimeColumn;
        public string ColumnName { get; }

        private StatisticColumn(string columnName, Func<Statistics, double> calc, bool isTimeColumn = true)
        {
            this.calc = calc;
            this.isTimeColumn = isTimeColumn;
            ColumnName = columnName;
        }

        public string GetValue(Summary summary, Benchmark benchmark) =>
            Format(summary[benchmark].ResultStatistics, summary.TimeUnit);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;

        private string Format(Statistics statistics, TimeUnit timeUnit)
        {
            if (statistics == null)
                return "NA";
            var value = calc(statistics);
            return isTimeColumn ? value.ToTimeStr(timeUnit) : value.ToStr();
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    }
}