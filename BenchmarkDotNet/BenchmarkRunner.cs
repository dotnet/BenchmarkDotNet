using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Settings;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IDictionary<string, object> defaultBenchmarkSettings, IEnumerable<IBenchmarkLogger> loggers)
        {
            DefaultBenchmarkSettings = defaultBenchmarkSettings ?? new Dictionary<string, object>();
            Logger = new BenchmarkCompositeLogger(loggers.ToArray());
        }

        public BenchmarkRunner(IDictionary<string, object> defaultBenchmarkSettings = null) :
            this(defaultBenchmarkSettings, new[] { new BenchmarkConsoleLogger() })
        {
        }

        public IDictionary<string, object> DefaultBenchmarkSettings { get; }
        public IBenchmarkLogger Logger { get; }

        public IEnumerable<IBenchmarkReport> RunCompetition(object benchmarkCompetition)
        {
            return RunCompetition(ObjectToBenchmarkCompetitionConverter.Convert(benchmarkCompetition));
        }

        public IEnumerable<IBenchmarkReport> RunCompetition(IEnumerable<IBenchmark> benchmarks)
        {
            Logger.WriteLineHeader("####### Competition: Start   #######");
            Logger.NewLine();
            var reports = benchmarks.Select(Run).ToList();
            Logger.WriteLineHeader("####### Competition: Results #######");
            Logger.NewLine();
            var reportStats = reports.Select(
                r => new
                {
                    r.Benchmark.Name,
                    Stat = new BenchmarkRunReportsStatistic("Target", r.Target)
                }).ToList();
            reportStats.Sort((a, b) => Math.Sign(a.Stat.Ticks.Median - b.Stat.Ticks.Median));
            var nameLength = reportStats.Max(r => r.Name.Length);
            var msLength = reportStats.Max(r => r.Stat.Milliseconds.Median.GetStrLength());
            var tickLength = reportStats.Max(r => r.Stat.Ticks.Median.GetStrLength());
            var stdDevLength = reportStats.Max(r => ((int)Math.Round(r.Stat.Milliseconds.StandardDeviation)).GetStrLength());
            var place = 1;
            for (int i = 0; i < reportStats.Count; i++)
            {
                var reportStat = reportStats[i];
                if (i > 0 && reportStats[i - 1].Stat.Milliseconds.Max < reportStats[i].Stat.Milliseconds.Min)
                    place++;
                Logger.WriteLineStatistic(
                    "{0} : {1}ms {2} ticks [Error = {3:00.00}%, StdDev = {4:0}ms] / Place #{5}",
                    reportStat.Name.PadRight(nameLength),
                    reportStat.Stat.Milliseconds.Median.ToInvariantString().PadLeft(msLength),
                    reportStat.Stat.Ticks.Median.ToInvariantString().PadLeft(tickLength),
                    reportStat.Stat.Milliseconds.Error * 100,
                    ((int)Math.Round(reportStat.Stat.Milliseconds.StandardDeviation)).ToInvariantString().PadLeft(stdDevLength),
                    place);
            }
            Logger.NewLine();
            Logger.WriteLineHeader("####### Competition: End     #######");
            return reports;
        }

        public IBenchmarkReport Run(IBenchmark benchmark)
        {
            Logger.WriteLineHeader("***** " + benchmark.Name + ": start *****");
            Logger.WriteLineExtraInfo(EnvironmentHelper.GetFullEnvironmentInfo());
            var settings = BenchmarkSettingsHelper.Union(benchmark.Settings, DefaultBenchmarkSettings);
            Logger.WriteLineHeader("Settings:");
            foreach (var setting in settings)
                Logger.WriteLineExtraInfo("  {0} = {1}", setting.Key, setting.Value);
            var hasInitialize = benchmark.Initialize != null;
            var hasClean = benchmark.Clean != null;
            if (hasInitialize || hasClean)
            {
                Logger.WriteLineHeader("Additional:");
                if (hasInitialize)
                    Logger.WriteLineExtraInfo(" Initialize action");
                if (hasClean)
                    Logger.WriteLineExtraInfo(" Clean action");
            }
            var processorAffinity = BenchmarkSettings.ProcessorAffinity.Get(settings);
            var highPriority = BenchmarkSettings.HighPriority.Get(settings);
            using (new BenchmarkScope(processorAffinity, highPriority))
            {
                IList<IBenchmarkRunReport> warmUp = new IBenchmarkRunReport[0];
                var maxWarmUpIterationCount = BenchmarkSettings.MaxWarmUpIterationCount.Get(settings);
                var targetIterationCount = BenchmarkSettings.TargetIterationCount.Get(settings);

                benchmark.Initialize?.Invoke();
                if (maxWarmUpIterationCount > 0)
                    warmUp = Run("WarmUp", benchmark.Action, maxWarmUpIterationCount, settings, StopWarmUpPredicate);
                var target = Run("Target", benchmark.Action, targetIterationCount, settings);
                benchmark.Clean?.Invoke();

                Logger.WriteLineHeader("***** " + benchmark.Name + ": end *****");
                Logger.NewLine();
                return new BenchmarkReport(benchmark, warmUp, target);
            }
        }

        private IList<IBenchmarkRunReport> Run(
            string name,
            Action action,
            uint iterationCount,
            IDictionary<string, object> settings,
            Func<IList<IBenchmarkRunReport>, IDictionary<string, object>, bool> stopPredicate = null)
        {
            var detailedMode = BenchmarkSettings.DetailedMode.Get(settings);
            var autoGcCollect = BenchmarkSettings.AutoGcCollect.Get(settings);
            Logger.WriteLineHeader(name + ":");
            var runReports = new List<IBenchmarkRunReport>();
            var stopwatch = new Stopwatch();
            for (int i = 0; i < iterationCount; i++)
            {
                if (autoGcCollect)
                    GcCollect();

                stopwatch.Reset();
                stopwatch.Start();
                action();
                stopwatch.Stop();

                var runReport = new BenchmarkRunReport(stopwatch);
                runReports.Add(runReport);
                BenchmarkReportHelper.LogRunReport(Logger, runReport);
                if (stopPredicate != null && stopPredicate(runReports, settings))
                    break;
            }
            var statistic = new BenchmarkRunReportsStatistic(name, runReports);
            BenchmarkReportHelper.Log(Logger, statistic, detailedMode);
            Logger.NewLine();
            return runReports;
        }

        private static bool StopWarmUpPredicate(IList<IBenchmarkRunReport> runList, IDictionary<string, object> settings)
        {
            var warmUpIterationCount = BenchmarkSettings.WarmUpIterationCount.Get(settings);
            var maxWarmUpError = BenchmarkSettings.MaxWarmUpError.Get(settings);
            if (runList.Count < warmUpIterationCount)
                return false;
            var lastRuns = runList.TakeLast(1).ToList();
            var lastRunsStatistic = new BenchmarkRunReportsStatistic("", lastRuns);
            var lastRunsError = lastRunsStatistic.Ticks.Error;
            return lastRunsError < maxWarmUpError;
        }

        private void GcCollect()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (AccessViolationException)
            {
                Logger.WriteError("AccessViolationException during GC.Collect()");
            }
        }

        protected static long PreparedEnvironmentTickCount { get; set; }

        private class BenchmarkScope : IDisposable
        {
            private readonly IntPtr originalProcessorAffinity;
            private readonly ProcessPriorityClass originalProcessPriorityClass;
            private readonly ThreadPriority originalThreadPriority;

            public BenchmarkScope(uint processorAffinity, bool highPriority)
            {
                PreparedEnvironmentTickCount = Environment.TickCount; // Prevents the JIT Compiler from optimizing Fkt calls away

                originalProcessorAffinity = Process.GetCurrentProcess().ProcessorAffinity;
                originalProcessPriorityClass = Process.GetCurrentProcess().PriorityClass;
                originalThreadPriority = Thread.CurrentThread.Priority;

                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(processorAffinity);
                if (highPriority)
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                }
            }

            public void Dispose()
            {
                Process.GetCurrentProcess().ProcessorAffinity = originalProcessorAffinity;
                Process.GetCurrentProcess().PriorityClass = originalProcessPriorityClass;
                Thread.CurrentThread.Priority = originalThreadPriority;
            }
        }
    }
}