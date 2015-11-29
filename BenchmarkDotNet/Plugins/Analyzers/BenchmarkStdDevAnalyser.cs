using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Plugins.Analyzers
{
    public class BenchmarkStdDevAnalyser : IBenchmarkAnalyser
    {
        public static readonly IBenchmarkAnalyser Default = new BenchmarkStdDevAnalyser();

        public string Name => "stddev";
        public string Description => "StdDev analyser";

        public IEnumerable<IBenchmarkAnalysisWarning> Analyze(IEnumerable<BenchmarkReport> reports)
        {
            var reportStats = reports.Where(r => r.Runs.Count > 0)
                .Select(r => new
                {
                    r.Benchmark,
                    Report = r,
                    Stat = new BenchmarkRunReportsStatistic("Target", r.Runs)
                }).ToList();
            foreach (var stat in reportStats)
            {
                var stdDev = stat.Stat.AverageTime.StandardDeviation;
                var mean = stat.Stat.AverageTime.Mean;
                var format = BenchmarkExporterHelper.GetTimeMeasurementFormattingFunc(new[] { stat.Stat });
                if (stdDev > 0.1 * mean)
                {
                    var percent = Math.Round(stdDev / mean * 100);
                    var message = $"StdDev ({format(stdDev)}) is {percent}% of Mean ({format(mean)}).";
                    yield return new BenchmarkAnalysisWarning("StdDev", message, stat.Report);
                }
            }
        }
    }
}