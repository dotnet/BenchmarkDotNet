using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using System.Linq;
using BenchmarkDotNet.Common;
using BenchmarkDotNet.Statistic;

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

        public IList<string> GetExtendedResults(IList<Tuple<BenchmarkReport, StatSummary>> reports, TimeUnit timeUnit)
        {
            var benchmarks = reports.Select(r => r.Item1.Benchmark).Distinct();
            var baselineCount = benchmarks.Count(b => b.Target.Baseline);
            if (baselineCount != 1)
                return null;

            var results = new List<string>(reports.Count);

            // Sanity check, make sure at least one benchmark is a Baseline!
            if (reports.Any(r => r.Item1.Benchmark.Target.Baseline) == false)
            {
                foreach (var item in reports)
                    results.Add("-");
                return results;
            }

            foreach (var item in reports)
            {
                double baseline = 0;
                if (item.Item1.Parameters != null)
                    baseline = reports.First(r => r.Item1.Benchmark.Target.Baseline &&
                                                  r.Item1.Parameters.IntParam == item.Item1.Parameters.IntParam)
                                      .Item2.Mean;
                else
                    baseline = reports.First(r => r.Item1.Benchmark.Target.Baseline)
                                      .Item2.Mean;
                var current = item.Item2.Mean;
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
