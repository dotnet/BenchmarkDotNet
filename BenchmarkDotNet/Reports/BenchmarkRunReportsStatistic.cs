using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Reports
{
    internal sealed class BenchmarkRunReportsStatistic : IBenchmarkRunReportsStatistic
    {
        public string Name { get; }
        public IBenchmarkMeasurementStatistic Ticks { get; }
        public IBenchmarkMeasurementStatistic Milliseconds { get; }

        public BenchmarkRunReportsStatistic(string name, IList<IBenchmarkRunReport> runReports)
        {
            Name = name;
            Ticks = new BenchmarkMeasurementStatistic("Ticks", runReports.Select(r => r.Ticks).ToArray());
            Milliseconds = new BenchmarkMeasurementStatistic("Ms", runReports.Select(r => r.Milliseconds).ToArray());
        }
    }
}