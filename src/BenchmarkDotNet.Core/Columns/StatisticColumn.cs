using System;
using System.Linq;
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
            Main,
            Quartile,
            Percentiles,
            Additional
        }
        
        public static readonly IColumn Mean = new StatisticColumn("Mean", "Arithmetic mean of all measurements",
            s => s.Mean, Priority.Main);

        public static readonly IColumn StdErr = new StatisticColumn("StdErr", "Standard error of all measurements",
            s => s.StandardError, Priority.Main);

        public static readonly IColumn StdDev = new StatisticColumn("StdDev", "Standard deviation of all measurements",
            s => s.StandardDeviation, Priority.Main);

        public static readonly IColumn Error = new StatisticColumn("Error", "Half of 99.9% confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, ConfidenceLevel.L999).Margin, Priority.Main);

        public static readonly IColumn OperationsPerSecond = new StatisticColumn("Op/s", "Operation per second",
            s => 1.0 * 1000 * 1000 * 1000 / s.Mean, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn Min = new StatisticColumn("Min", "Minimum",
            s => s.Min, Priority.Quartile);

        public static readonly IColumn Q1 = new StatisticColumn("Q1", "Quartile 1 (25th percentile)",
            s => s.Q1, Priority.Quartile);

        public static readonly IColumn Median = new StatisticColumn("Median", "Value separating the higher half of all measurements (50th percentile)",
            s => s.Median, Priority.Quartile);

        public static readonly IColumn Q3 = new StatisticColumn("Q3", "Quartile 3 (75th percentile)",
            s => s.Q3, Priority.Quartile);

        public static readonly IColumn Max = new StatisticColumn("Max", "Maximum", s => s.Max, Priority.Quartile);

        public static readonly IColumn Skewness = new StatisticColumn("Skewness", "Measure of the asymmetry (third standardized moment)",
            s => s.Skewness, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn Kurtosis = new StatisticColumn("Kurtosis", "Measure of the tailedness ( fourth standardized moment)",
            s => s.Kurtosis, Priority.Additional, UnitType.Dimensionless);

        /// <summary>
        /// See http://www.brendangregg.com/FrequencyTrails/modes.html
        /// </summary>
        public static readonly IColumn MValue = new StatisticColumn("MValue", "Modal value, see http://www.brendangregg.com/FrequencyTrails/modes.html",
            MathHelper.CalculateMValue, Priority.Additional, UnitType.Dimensionless);
        
        public static readonly IColumn Iterations = new StatisticColumn("Iterations", "Number of target iterations",
            s => s.N, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn P0 = CreatePercentileColumn(0, s => s.Percentiles.P0);
        public static readonly IColumn P25 = CreatePercentileColumn(25, s => s.Percentiles.P25);
        public static readonly IColumn P50 = CreatePercentileColumn(50, s => s.Percentiles.P50);
        public static readonly IColumn P67 = CreatePercentileColumn(67, s => s.Percentiles.P67);
        public static readonly IColumn P80 = CreatePercentileColumn(80, s => s.Percentiles.P80);
        public static readonly IColumn P85 = CreatePercentileColumn(85, s => s.Percentiles.P85);
        public static readonly IColumn P90 = CreatePercentileColumn(90, s => s.Percentiles.P90);
        public static readonly IColumn P95 = CreatePercentileColumn(95, s => s.Percentiles.P95);
        public static readonly IColumn P100 = CreatePercentileColumn(100, s => s.Percentiles.P100);

        public static IColumn CiLower(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Lower", $"Lower bound of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Lower, Priority.Additional);

        public static IColumn CiUpper(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Upper", $"Upper bound of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Upper, Priority.Additional);

        public static IColumn CiError(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Margin", $"Half of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Margin, Priority.Additional);


        public static readonly IColumn[] AllStatistics = { Mean, StdErr, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };

        private readonly Func<Statistics, double> calc;
        public string Id => nameof(StatisticColumn) + "." + ColumnName;
        public string ColumnName { get; }
        private readonly Priority priority;
        private readonly UnitType type;

        private StatisticColumn(string columnName, string legend, Func<Statistics, double> calc, Priority priority, UnitType type = UnitType.Time)
        {
            this.calc = calc;
            this.priority = priority;
            this.type = type;
            ColumnName = columnName;
            Legend = legend;
        }

        public string GetValue(Summary summary, Benchmark benchmark)
            => Format(summary, summary[benchmark].ResultStatistics, SummaryStyle.Default);

        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style)
            => Format(summary, summary[benchmark].ResultStatistics, style);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public int PriorityInCategory => (int) priority;
        public bool IsNumeric => true;
        public UnitType UnitType => type;
        public string Legend { get; }

        private string Format(Summary summary, Statistics statistics, ISummaryStyle style)
        {
            if (statistics == null)
                return "NA";

            var allValues = summary
                .Reports
                .Where(r => r.ResultStatistics != null)
                .Select(r => calc(r.ResultStatistics))
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .Select(v => type == UnitType.Time ? v / style.TimeUnit.NanosecondAmount : v)
                .ToList();
            double minValue = allValues.Any() ? allValues.Min() : 0;
            bool allValuesAreZeros = allValues.All(v => Math.Abs(v) < 1e-9);
            string format = "N" + (allValuesAreZeros ? 1 : GetBestAmountOfDecimalDigits(minValue));

            double value = calc(statistics);
            if (double.IsNaN(value))
                return "NA";
            return type == UnitType.Time ? value.ToTimeStr(style.TimeUnit, 1, style.PrintUnitsInContent, format: format) : value.ToStr(format);
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;

        private static IColumn CreatePercentileColumn(int percentiles, Func<Statistics, double> calc) => new StatisticColumn(
            "P" + percentiles, "Percentile " + percentiles, calc, Priority.Percentiles);

        // TODO: Move to a better place
        public static int GetBestAmountOfDecimalDigits(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            if (value < 1 - 1e-9)
                return 4;
            return MathHelper.Clamp((int) Math.Truncate(-Math.Log10(value)) + 3, 1, 4);
        }
    }
}