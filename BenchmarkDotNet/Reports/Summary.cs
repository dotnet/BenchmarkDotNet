using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    public class Summary
    {
        public string Title { get; }
        public Benchmark[] Benchmarks { get; }
        public IDictionary<Benchmark, BenchmarkReport> Reports { get; }
        public TimeUnit TimeUnit { get; private set; }
        public EnvironmentInfo HostEnvironmentInfo { get; }
        public IConfig Config { get; }
        public string ResultsDirectoryPath { get; }
        public SummaryTable Table { get; }
        public TimeSpan TotalTime { get; }

        private Dictionary<IJob, string> ShortInfos { get; }
        private Lazy<IJob[]> Jobs { get; }

        public Summary(string title, IList<BenchmarkReport> reports, EnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, TimeSpan totalTime)
        {
            Title = title;
            HostEnvironmentInfo = hostEnvironmentInfo;
            Config = config;
            ResultsDirectoryPath = resultsDirectoryPath;
            TotalTime = totalTime;

            Reports = new Dictionary<Benchmark, BenchmarkReport>();
            Benchmarks = reports.Select(r => r.Benchmark).ToArray();
            foreach (var report in reports)
                Reports[report.Benchmark] = report;

            TimeUnit = TimeUnit.GetBestTimeUnit(reports.Where(r => r.ResultStatistics != null).Select(r => r.ResultStatistics.Mean).ToArray());
            Table = new SummaryTable(this);
            ShortInfos = new Dictionary<IJob, string>();
            Jobs = new Lazy<IJob[]>(() => Benchmarks.Select(b => b.Job).ToArray());
        }

        internal string GetJobShortInfo(IJob job)
        {
            string result;
            if (!ShortInfos.TryGetValue(job, out result))
            {
                ShortInfos[job] = result = job.GetShortInfo(Jobs.Value);
            }

            return result;
        }
    }
}