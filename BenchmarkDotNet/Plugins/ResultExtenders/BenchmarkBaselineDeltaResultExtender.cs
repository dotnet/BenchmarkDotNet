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
                {
                    var firstMatch = 
                        reports.FirstOrDefault(r => r.Item1.Benchmark.Target.Baseline &&
                                                    r.Item1.Parameters.IntParam == item.Item1.Parameters.IntParam);
                    if (firstMatch != null)
                        baseline = firstMatch.Item2.OperationsPerSeconds.Mean;
                }
                else
                {
                    var firstMatch = reports.First(r => r.Item1.Benchmark.Target.Baseline);
                    if (firstMatch != null)
                        baseline = firstMatch.Item2.OperationsPerSeconds.Mean;
                }

                var current = item.Item2.OperationsPerSeconds.Mean;
                double diff = 0;
                if (baseline != 0) // This can happen if we found no matching result
                    diff = (current - baseline) / baseline * 100.0;

                if (item.Item1.Benchmark.Target.Baseline)
                    results.Add("-");
                else
                    results.Add(diff.ToString("N1") + "%");
            }
            return results;
        }
    }
}
