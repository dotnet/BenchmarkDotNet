﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public class Summary
    {
        [PublicAPI] public string Title { get; }
        [PublicAPI] public string ResultsDirectoryPath { get; }
        [PublicAPI] public string LogFilePath { get; }
        [PublicAPI] public HostEnvironmentInfo HostEnvironmentInfo { get; }
        [PublicAPI] public TimeSpan TotalTime { get; }
        [PublicAPI, CanBeNull] public SummaryStyle Style { get; }
        [PublicAPI] public IOrderer Orderer { get; }
        [PublicAPI] public SummaryTable Table { get; }
        [PublicAPI] public string AllRuntimes { get; }
        [PublicAPI] public ImmutableArray<ValidationError> ValidationErrors { get; }

        [PublicAPI] public ImmutableArray<BenchmarkCase> BenchmarksCases { get; }
        [PublicAPI] public ImmutableArray<BenchmarkReport> Reports { get; }

        private ImmutableDictionary<BenchmarkCase, BenchmarkReport> ReportMap {get; }
        private BaseliningStrategy BaseliningStrategy {get; }

        internal DisplayPrecisionManager DisplayPrecisionManager { get; }

        public Summary(
            string title,
            ImmutableArray<BenchmarkReport> reports,
            HostEnvironmentInfo hostEnvironmentInfo,
            string resultsDirectoryPath,
            string logFilePath,
            TimeSpan totalTime,
            CultureInfo cultureInfo,
            ImmutableArray<ValidationError> validationErrors)
        {
            Title = title;
            ResultsDirectoryPath = resultsDirectoryPath;
            LogFilePath = logFilePath;
            HostEnvironmentInfo = hostEnvironmentInfo;
            TotalTime = totalTime;
            ValidationErrors = validationErrors;

            ReportMap = reports.ToImmutableDictionary(report => report.BenchmarkCase, report => report);

            DisplayPrecisionManager = new DisplayPrecisionManager(this);
            Orderer = GetConfiguredOrdererOrDefaultOne(reports.Select(report => report.BenchmarkCase.Config));
            BenchmarksCases = Orderer.GetSummaryOrder(reports.Select(report => report.BenchmarkCase).ToImmutableArray(), this).ToImmutableArray(); // we sort it first
            Reports = BenchmarksCases.Select(b => ReportMap[b]).ToImmutableArray(); // we use sorted collection to re-create reports list
            BaseliningStrategy = BaseliningStrategy.Create(BenchmarksCases);
            Style = GetConfiguredSummaryStyleOrNull(BenchmarksCases)?.WithCultureInfo(cultureInfo);
            Table = GetTable(Style);
            AllRuntimes = BuildAllRuntimes(HostEnvironmentInfo, Reports);
        }

        [PublicAPI] public bool HasReport(BenchmarkCase benchmarkCase) => ReportMap.ContainsKey(benchmarkCase);

        /// <summary>
        /// Returns a report for the given benchmark or null if there is no a corresponded report.
        /// </summary>
        public BenchmarkReport this[BenchmarkCase benchmarkCase] => ReportMap.GetValueOrDefault(benchmarkCase);

        public bool HasCriticalValidationErrors => ValidationErrors.Any(validationError => validationError.IsCritical);

        public int GetNumberOfExecutedBenchmarks() => Reports.Count(report => report.ExecuteResults.Any(result => result.FoundExecutable));

        internal static Summary NothingToRun(string title, string resultsDirectoryPath, string logFilePath)
            => new Summary(title, ImmutableArray<BenchmarkReport>.Empty, HostEnvironmentInfo.GetCurrent(), resultsDirectoryPath, logFilePath, TimeSpan.Zero, DefaultCultureInfo.Instance, ImmutableArray<ValidationError>.Empty);

        internal static Summary ValidationFailed(string title, string resultsDirectoryPath, string logFilePath, ImmutableArray<ValidationError> validationErrors)
            => new Summary(title, ImmutableArray<BenchmarkReport>.Empty, HostEnvironmentInfo.GetCurrent(), resultsDirectoryPath, logFilePath, TimeSpan.Zero, DefaultCultureInfo.Instance, validationErrors);

        internal static Summary Join(List<Summary> summaries, ClockSpan clockSpan)
            => new Summary(
                $"BenchmarkRun-joined-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
                summaries.SelectMany(summary => summary.Reports).ToImmutableArray(),
                HostEnvironmentInfo.GetCurrent(),
                summaries.First().ResultsDirectoryPath,
                summaries.First().LogFilePath,
                clockSpan.GetTimeSpan(),
                summaries.First().GetCultureInfo(),
                summaries.SelectMany(summary => summary.ValidationErrors).ToImmutableArray());

        internal static string BuildAllRuntimes(HostEnvironmentInfo hostEnvironmentInfo, IEnumerable<BenchmarkReport> reports)
        {
            var jobRuntimes = new Dictionary<string, string>(); // JobId -> Runtime
            var orderedJobs = new List<string> { "[Host]" };

            jobRuntimes["[Host]"] = hostEnvironmentInfo.GetRuntimeInfo();

            foreach (var benchmarkReport in reports)
            {
                string runtime = benchmarkReport.GetRuntimeInfo();
                if (runtime != null)
                {
                    string jobId = benchmarkReport.BenchmarkCase.Job.ResolvedId;

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

        internal SummaryTable GetTable(SummaryStyle style) => new SummaryTable(this, style);

        [CanBeNull]
        public string GetLogicalGroupKey(BenchmarkCase benchmarkCase)
            => Orderer.GetLogicalGroupKey(BenchmarksCases, benchmarkCase);

        public bool IsBaseline(BenchmarkCase benchmarkCase)
            => BaseliningStrategy.IsBaseline(benchmarkCase);

        [CanBeNull]
        public BenchmarkCase GetBaseline(string logicalGroupKey)
            => BenchmarksCases
                .Where(b => GetLogicalGroupKey(b) == logicalGroupKey)
                .FirstOrDefault(IsBaseline);

        [NotNull]
        public IEnumerable<BenchmarkCase> GetNonBaselines(string logicalGroupKey)
            => BenchmarksCases
                .Where(b => GetLogicalGroupKey(b) == logicalGroupKey)
                .Where(b => !IsBaseline(b));

        public bool HasBaselines() => BenchmarksCases.Any(IsBaseline);

        private static IOrderer GetConfiguredOrdererOrDefaultOne(IEnumerable<ImmutableConfig> configs)
            => configs
                   .Where(config => config.Orderer != DefaultOrderer.Instance)
                   .Select(config => config.Orderer)
                   .Distinct()
                   .SingleOrDefault()
               ?? DefaultOrderer.Instance;

        private static SummaryStyle GetConfiguredSummaryStyleOrNull(ImmutableArray<BenchmarkCase> benchmarkCases)
            => benchmarkCases.Select(benchmark => benchmark.Config.SummaryStyle).Distinct().SingleOrDefault();
    }
}
