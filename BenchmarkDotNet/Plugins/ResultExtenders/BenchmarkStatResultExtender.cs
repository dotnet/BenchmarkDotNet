using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Plugins.ResultExtenders
{
    public class BenchmarkStatResultExtender : IBenchmarkResultExtender
    {
        public static readonly IBenchmarkResultExtender AvrTime = new BenchmarkStatResultExtender("AvrTime", s => s.Mean);
        public static readonly IBenchmarkResultExtender Error = new BenchmarkStatResultExtender("Error", s => s.StandardError);

        public static readonly IBenchmarkResultExtender StdDev = new BenchmarkStatResultExtender("StdDev", s => s.StandardDeviation);
        public static readonly IBenchmarkResultExtender OperationPerSecond = new BenchmarkStatResultExtender("Op/s", s => 1.0 * 1000 * 1000 * 1000 / s.Mean, false);

        public static readonly IBenchmarkResultExtender Min = new BenchmarkStatResultExtender("Min", s => s.Min);
        public static readonly IBenchmarkResultExtender Q1 = new BenchmarkStatResultExtender("Q1", s => s.Q1);
        public static readonly IBenchmarkResultExtender Median = new BenchmarkStatResultExtender("Median", s => s.Median);
        public static readonly IBenchmarkResultExtender Q3 = new BenchmarkStatResultExtender("Q3", s => s.Q3);
        public static readonly IBenchmarkResultExtender Max = new BenchmarkStatResultExtender("Max", s => s.Max);

        private readonly Func<StatSummary, double> calc;
        private readonly bool isTimeColumn;
        public string ColumnName { get; }

        private BenchmarkStatResultExtender(string columnName, Func<StatSummary, double> calc, bool isTimeColumn = true)
        {
            this.calc = calc;
            this.isTimeColumn = isTimeColumn;
            ColumnName = columnName;
        }

        public IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, StatSummary>> reports, TimeUnit timeUnit) =>
            reports.Select(r => Format(r.Item2, timeUnit)).ToList();

        private string Format(StatSummary stat, TimeUnit timeUnit)
        {
            var value = calc(stat);
            return isTimeColumn ? value.ToTimeStr(timeUnit) : value.ToStr();
        }
    }
}