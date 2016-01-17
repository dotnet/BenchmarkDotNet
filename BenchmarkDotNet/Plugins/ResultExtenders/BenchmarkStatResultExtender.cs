using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Common;
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

        public static readonly IBenchmarkResultExtender Min = new BenchmarkStatResultExtender("Min", s => s.Min);
        public static readonly IBenchmarkResultExtender Q1 = new BenchmarkStatResultExtender("Q1", s => s.Q1);
        public static readonly IBenchmarkResultExtender Median = new BenchmarkStatResultExtender("Median", s => s.Median);
        public static readonly IBenchmarkResultExtender Q3 = new BenchmarkStatResultExtender("Q3", s => s.Q3);
        public static readonly IBenchmarkResultExtender Max = new BenchmarkStatResultExtender("Max", s => s.Max);

        private readonly Func<StatSummary, double> calc;
        public string ColumnName { get; }

        public BenchmarkStatResultExtender(string columnName, Func<StatSummary, double> calc)
        {
            this.calc = calc;
            ColumnName = columnName;
        }

        public IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, StatSummary>> reports, TimeUnit timeUnit) =>
            reports.Select(r => calc(r.Item2).ToTimeStr(timeUnit)).ToList();
    }
}