using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkRunReportsStatistic
    {
        public string Name { get; }
        public BenchmarkMeasurementStatistic Values { get; }
        public string Unit { get; }

        public BenchmarkRunReportsStatistic(string name, IList<BenchmarkRunReport> runReports)
        {
            Name = name;
            Values = new BenchmarkMeasurementStatistic("Values", runReports.Select(r => r.Value).ToArray());
            Unit = runReports.FirstOrDefault()?.Unit ?? "Undef";
        }
    }
}