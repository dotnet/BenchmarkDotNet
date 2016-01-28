using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class StatisticColumn : IColumn
    {
        public static readonly IColumn Time = new StatisticColumn("Time", s => s.Mean);
        public static readonly IColumn Error = new StatisticColumn("Error", s => s.StandardError);

        public static readonly IColumn StdDev = new StatisticColumn("StdDev", s => s.StandardDeviation);
        public static readonly IColumn OperationsPerSecond = new StatisticColumn("Op/s", s => 1.0 * 1000 * 1000 * 1000 / s.Mean, false);

        public static readonly IColumn Min = new StatisticColumn("Min", s => s.Min);
        public static readonly IColumn Q1 = new StatisticColumn("Q1", s => s.Q1);
        public static readonly IColumn Median = new StatisticColumn("Median", s => s.Median);
        public static readonly IColumn Q3 = new StatisticColumn("Q3", s => s.Q3);
        public static readonly IColumn Max = new StatisticColumn("Max", s => s.Max);

        public static readonly IColumn[] AllStatistics = { Time, Error, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };

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
            Format(summary.Reports[benchmark].ResultStatistics, summary.TimeUnit);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;

        private string Format(Statistics statistics, TimeUnit timeUnit)
        {
            if (statistics == null)
                return "NA";
            var value = calc(statistics);
            return isTimeColumn ? value.ToTimeStr(timeUnit) : value.ToStr();
        }

        public override string ToString() => ColumnName;
    }
}