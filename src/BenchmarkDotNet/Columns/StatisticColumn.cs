using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using Perfolizer.Common;
using Perfolizer.Horology;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.Multimodality;

namespace BenchmarkDotNet.Columns
{
    public interface IStatisticColumn : IColumn
    {
        List<double> GetAllValues(Summary summary, SummaryStyle style);
    }

    public class StatisticColumn : IStatisticColumn
    {
        private enum Priority
        {
            Main,
            Quartile,
            Percentiles,
            Additional
        }

        public static readonly IStatisticColumn Mean = new StatisticColumn(Column.Mean, "Arithmetic mean of all measurements",
            s => s.Mean, Priority.Main);

        public static readonly IColumn StdErr = new StatisticColumn(Column.StdErr, "Standard error of all measurements",
            s => s.StandardError, Priority.Main, parentColumn: Mean);

        public static readonly IColumn StdDev = new StatisticColumn(Column.StdDev, "Standard deviation of all measurements",
            s => s.StandardDeviation, Priority.Main, parentColumn: Mean);

        public static readonly IColumn Error = new StatisticColumn(Column.Error, "Half of 99.9% confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, ConfidenceLevel.L999).Margin, Priority.Main, parentColumn: Mean);

        public static readonly IColumn OperationsPerSecond = new StatisticColumn(Column.OperationPerSecond, "Operation per second",
            s => 1.0 * 1000 * 1000 * 1000 / s.Mean, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn Min = new StatisticColumn(Column.Min, "Minimum",
            s => s.Min, Priority.Quartile);

        public static readonly IColumn Q1 = new StatisticColumn(Column.Q1, "Quartile 1 (25th percentile)",
            s => s.Q1, Priority.Quartile);

        public static readonly IColumn Median = new StatisticColumn(Column.Median, "Value separating the higher half of all measurements (50th percentile)",
            s => s.Median, Priority.Quartile);

        public static readonly IColumn Q3 = new StatisticColumn(Column.Q3, "Quartile 3 (75th percentile)",
            s => s.Q3, Priority.Quartile);

        public static readonly IColumn Max = new StatisticColumn(Column.Max, "Maximum", s => s.Max, Priority.Quartile);

        public static readonly IColumn Skewness = new StatisticColumn(Column.Skewness, "Measure of the asymmetry (third standardized moment)",
            s => s.Skewness, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn Kurtosis = new StatisticColumn(Column.Kurtosis, "Measure of the tailedness ( fourth standardized moment)",
            s => s.Kurtosis, Priority.Additional, UnitType.Dimensionless);

        /// <summary>
        /// See http://www.brendangregg.com/FrequencyTrails/modes.html
        /// </summary>
        public static readonly IColumn MValue = new StatisticColumn(Column.MValue, "Modal value, see http://www.brendangregg.com/FrequencyTrails/modes.html",
            s => MValueCalculator.Calculate(s.OriginalValues), Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn Iterations = new StatisticColumn(Column.Iterations, "Number of target iterations",
            s => s.N, Priority.Additional, UnitType.Dimensionless);

        public static readonly IColumn P0 = CreatePercentileColumn(0, Column.P0, s => s.Percentiles.P0);
        public static readonly IColumn P25 = CreatePercentileColumn(25, Column.P25, s => s.Percentiles.P25);
        public static readonly IColumn P50 = CreatePercentileColumn(50, Column.P50, s => s.Percentiles.P50);
        public static readonly IColumn P67 = CreatePercentileColumn(67, Column.P67, s => s.Percentiles.P67);
        public static readonly IColumn P80 = CreatePercentileColumn(80, Column.P80, s => s.Percentiles.P80);
        public static readonly IColumn P85 = CreatePercentileColumn(85, Column.P85, s => s.Percentiles.P85);
        public static readonly IColumn P90 = CreatePercentileColumn(90, Column.P90, s => s.Percentiles.P90);
        public static readonly IColumn P95 = CreatePercentileColumn(95, Column.P95, s => s.Percentiles.P95);
        public static readonly IColumn P100 = CreatePercentileColumn(100, Column.P100, s => s.Percentiles.P100);

        [PublicAPI]
        public static IColumn CiLower(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Lower", $"Lower bound of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Lower, Priority.Additional);

        [PublicAPI]
        public static IColumn CiUpper(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Upper", $"Upper bound of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Upper, Priority.Additional);

        [PublicAPI]
        public static IColumn CiError(ConfidenceLevel level) => new StatisticColumn(
            $"CI{level.ToPercentStr()} Margin", $"Half of {level.ToPercentStr()} confidence interval",
            s => new ConfidenceInterval(s.Mean, s.StandardError, s.N, level).Margin, Priority.Additional);


        public static readonly IColumn[] AllStatistics = { Mean, StdErr, StdDev, OperationsPerSecond, Min, Q1, Median, Q3, Max };

        private readonly Func<Statistics, double> calc;
        public string Id => nameof(StatisticColumn) + "." + ColumnName;
        public string ColumnName { get; }
        private readonly Priority priority;
        private readonly IStatisticColumn parentColumn;

        private StatisticColumn(string columnName, string legend, Func<Statistics, double> calc, Priority priority, UnitType type = UnitType.Time,
            IStatisticColumn? parentColumn = null)
        {
            this.calc = calc;
            this.priority = priority;
            this.parentColumn = parentColumn;
            UnitType = type;
            ColumnName = columnName;
            Legend = legend;
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
            => Format(summary, benchmarkCase.Config, summary[benchmarkCase].ResultStatistics, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            => Format(summary, benchmarkCase.Config, summary[benchmarkCase].ResultStatistics, style);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public int PriorityInCategory => (int) priority;
        public bool IsNumeric => true;
        public UnitType UnitType { get; }

        public string Legend { get; }

        public List<double> GetAllValues(Summary summary, SummaryStyle style)
            => summary.Reports
                .Where(r => r.ResultStatistics != null)
                .Select(r => calc(r.ResultStatistics))
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .Select(v => UnitType == UnitType.Time ? v / style.TimeUnit.NanosecondAmount : v)
                .ToList();

        private string Format(Summary summary, ImmutableConfig config, Statistics statistics, SummaryStyle style)
        {
            if (statistics == null)
                return "NA";

            int precision = summary.DisplayPrecisionManager.GetPrecision(style, this, parentColumn);
            string format = "N" + precision;

            double value = calc(statistics);
            if (double.IsNaN(value))
                return "NA";
            return UnitType == UnitType.Time
                ? TimeInterval.FromNanoseconds(value)
                    .ToString(
                        style.TimeUnit,
                        style.CultureInfo,
                        format,
                        UnitPresentation.FromVisibility(style.PrintUnitsInContent))
                : value.ToString(format, style.CultureInfo);
        }

        public override string ToString() => ColumnName;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        private static IColumn CreatePercentileColumn(int percentiles, string columnName, Func<Statistics, double> calc) => new StatisticColumn(
            columnName, "Percentile " + percentiles, calc, Priority.Percentiles);
    }
}