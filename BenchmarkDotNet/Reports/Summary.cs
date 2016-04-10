using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    public class Summary
    {
        public string Title { get; }
        public Benchmark[] Benchmarks { get; }
        public BenchmarkReport[] Reports { get; }
        public TimeUnit TimeUnit { get; private set; }
        public EnvironmentInfo HostEnvironmentInfo { get; }
        public IConfig Config { get; }
        public string ResultsDirectoryPath { get; }
        public SummaryTable Table { get; }
        public TimeSpan TotalTime { get; }

        private Dictionary<IJob, string> ShortInfos { get; }
        private Lazy<IJob[]> Jobs { get; }
        private IDictionary<Benchmark, BenchmarkReport> reportMap { get; }

        public BenchmarkReport this[Benchmark benchmark] => reportMap[benchmark];

        public Summary(string title, IList<BenchmarkReport> reports, EnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, TimeSpan totalTime)
        {
            Title = title;
            HostEnvironmentInfo = hostEnvironmentInfo;
            Config = config;
            ResultsDirectoryPath = resultsDirectoryPath;
            TotalTime = totalTime;

            Benchmarks = reports.Select(r => r.Benchmark).ToArray();
            reportMap = new Dictionary<Benchmark, BenchmarkReport>();
            foreach (var report in reports)
                reportMap[report.Benchmark] = report;
            Reports = Benchmarks.Select(b => reportMap[b]).ToArray();

            var orderProvider = config.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            Benchmarks = orderProvider.GetSummaryOrder(Benchmarks, this).ToArray();
            Reports = Benchmarks.Select(b => reportMap[b]).ToArray();

            TimeUnit = TimeUnit.GetBestTimeUnit(reports.Where(r => r.ResultStatistics != null).Select(r => r.ResultStatistics.Mean).ToArray());
            Table = new SummaryTable(this);
            ShortInfos = new Dictionary<IJob, string>();
            Jobs = new Lazy<IJob[]>(() => Benchmarks.Select(b => b.Job).ToArray());
        }

        internal string GetJobShortInfo(IJob job)
        {
            string result;
            if (!ShortInfos.TryGetValue(job, out result))
                ShortInfos[job] = result = job.GetShortInfo(Jobs.Value);

            return result;
        }
    }
}