using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Reports
{
    public class Summary
    {
        public string Title { get; }
        public Benchmark[] Benchmarks { get; }
        public BenchmarkReport[] Reports { get; }
        public ISummaryStyle Style { get; }
        public HostEnvironmentInfo HostEnvironmentInfo { get; }
        public IConfig Config { get; }
        public string ResultsDirectoryPath { get; }
        public SummaryTable Table { get; }
        public TimeSpan TotalTime { get; }
        public ValidationError[] ValidationErrors { get; }
        public string AllRuntimes { get; }

        private readonly Dictionary<Job, string> shortInfos;
        private readonly Lazy<Job[]> jobs;
        private readonly Dictionary<Benchmark, BenchmarkReport> reportMap = new Dictionary<Benchmark, BenchmarkReport>();

        public bool HasReport(Benchmark benchmark) => reportMap.ContainsKey(benchmark);

        /// <summary>
        /// Returns a report for the given benchmark or null if there is no a corresponded report.
        /// </summary>        
        public BenchmarkReport this[Benchmark benchmark] => reportMap.GetValueOrDefault(benchmark);

        public bool HasCriticalValidationErrors => ValidationErrors.Any(validationError => validationError.IsCritical);

        public Summary(string title, IList<BenchmarkReport> reports, HostEnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, TimeSpan totalTime, ValidationError[] validationErrors)
            : this(title, hostEnvironmentInfo, config, resultsDirectoryPath, totalTime, validationErrors)
        {
            Benchmarks = reports.Select(r => r.Benchmark).ToArray();
            foreach (var report in reports)
                reportMap[report.Benchmark] = report;
            Reports = Benchmarks.Select(b => reportMap[b]).ToArray();

            var orderProvider = config.GetOrderProvider() ?? DefaultOrderProvider.Instance;
            Benchmarks = orderProvider.GetSummaryOrder(Benchmarks, this).ToArray();
            Reports = Benchmarks.Select(b => reportMap[b]).ToArray();

            Table = GetTable(config.GetSummaryStyle());
            shortInfos = new Dictionary<Job, string>();
            jobs = new Lazy<Job[]>(() => Benchmarks.Select(b => b.Job).ToArray());
            AllRuntimes = BuildAllRuntimes();
        }

        private Summary(string title, HostEnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, TimeSpan totalTime, ValidationError[] validationErrors, Benchmark[] benchmarks, BenchmarkReport[] reports)
            : this(title, hostEnvironmentInfo, config, resultsDirectoryPath, totalTime, validationErrors)
        {
            Benchmarks = benchmarks;
            Table = GetTable(config.GetSummaryStyle());
            Reports = reports ?? Array.Empty<BenchmarkReport>();
        }

        private Summary(string title, HostEnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, TimeSpan totalTime, ValidationError[] validationErrors)
        {
            Title = title;
            HostEnvironmentInfo = hostEnvironmentInfo;
            Config = config;
            ResultsDirectoryPath = resultsDirectoryPath;
            TotalTime = totalTime;
            ValidationErrors = validationErrors;
            Reports = Array.Empty<BenchmarkReport>();
        }

        internal SummaryTable GetTable(ISummaryStyle style)
        {
            return new SummaryTable(this, style);
        }

        internal static Summary CreateFailed(Benchmark[] benchmarks, string title, HostEnvironmentInfo hostEnvironmentInfo, IConfig config, string resultsDirectoryPath, ValidationError[] validationErrors)
        {
            return new Summary(title, hostEnvironmentInfo, config, resultsDirectoryPath, TimeSpan.Zero, validationErrors, benchmarks, Array.Empty<BenchmarkReport>());
        }

        private string BuildAllRuntimes()
        {
            var jobRuntimes = new Dictionary<string, string>(); // JobId -> Runtime
            var orderedJobs = new List<string>();

            orderedJobs.Add("[Host]");
            jobRuntimes["[Host]"] = HostEnvironmentInfo.GetRuntimeInfo();

            foreach (var benchmarkReport in Reports)
            {
                string runtime = benchmarkReport.GetRuntimeInfo();
                if (runtime != null)
                {
                    string jobId = benchmarkReport.Benchmark.Job.ResolvedId;

                    if (!jobRuntimes.ContainsKey(jobId))
                    {
                        orderedJobs.Add(jobId);
                        jobRuntimes[jobId] = runtime;
                    }
                }
            }

            int jobIdMaxWidth = orderedJobs.Max(j => j.ToString().Length);

            var lines = orderedJobs.Select(jobId => $"  {jobId.PadRight(jobIdMaxWidth)} : {jobRuntimes[jobId]}");
            return string.Join(Environment.NewLine, lines);
        }
    }
}