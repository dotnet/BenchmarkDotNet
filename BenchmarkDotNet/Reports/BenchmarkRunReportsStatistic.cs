using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkRunReportsStatistic
    {
        public string Name { get; }
        public BenchmarkMeasurementStatistic AverageTime { get; }
        public BenchmarkMeasurementStatistic OperationsPerSeconds { get; }
        public string Unit { get; }

        public BenchmarkRunReportsStatistic(string name, IList<BenchmarkRunReport> runReports)
        {
            Name = name;
            AverageTime = new BenchmarkMeasurementStatistic(runReports.Select(r => r.AverageNanoseconds).ToArray());
            OperationsPerSeconds = new BenchmarkMeasurementStatistic(runReports.Select(r => r.OpsPerSecond).ToArray());
        }
    }
}