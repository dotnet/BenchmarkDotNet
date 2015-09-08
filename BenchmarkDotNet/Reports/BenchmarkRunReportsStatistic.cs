using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Statistic;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkRunReportsStatistic
    {
        public string Name { get; }
        public StatSummary AverageTime { get; }
        public StatSummary OperationsPerSeconds { get; }
        public string Unit { get; }

        public BenchmarkRunReportsStatistic(string name, IList<BenchmarkRunReport> runReports)
        {
            Name = name;
            AverageTime = new StatSummary(runReports.Select(r => r.AverageNanoseconds));
            OperationsPerSeconds = new StatSummary(runReports.Select(r => r.OpsPerSecond));
        }
    }
}