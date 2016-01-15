using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using System.Linq;

namespace BenchmarkDotNet.Plugins.ResultExtenders
{
    // Built to implement https://github.com/PerfDotNet/BenchmarkDotNet/issues/64
    public class BenchmarkBaselineDeltaResultExtender : IBenchmarkResultExtender
    {
        public string ColumnName { get; private set; }

        public BenchmarkBaselineDeltaResultExtender()
        {
            ColumnName = $"+/- Delta";
        }

        public IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, BenchmarkRunReportsStatistic>> reports)
        {
            var results = new List<string>(reports.Count);
            foreach (var item in reports)
            {
                double baseline = 0;
                if (item.Item1.Parameters != null)
                    baseline = reports.First(r => r.Item1.Benchmark.Target.Baseline &&
                                                  r.Item1.Parameters.IntParam == item.Item1.Parameters.IntParam)
                                      .Item2.OperationsPerSeconds.Mean;
                else
                    baseline = reports.First(r => r.Item1.Benchmark.Target.Baseline)
                                      .Item2.OperationsPerSeconds.Mean;
                var current = item.Item2.OperationsPerSeconds.Mean;
                var diff = (current - baseline) / baseline * 100.0;
                if (item.Item1.Benchmark.Target.Baseline)
                    results.Add("-");
                else
                    results.Add(diff.ToString("N1") + "%");
            }
            return results;
        }
    }
}
