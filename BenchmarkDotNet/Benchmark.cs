using System;
using System.Diagnostics;
using System.Threading;

namespace BenchmarkDotNet
{
    public class Benchmark
    {
        public int MaxWarmUpIterationCount { get; set; }
        public int WarmUpIterationCount { get; set; }
        public double MaxWarmUpError { get; set; }
        public int ResultIterationCount { get; set; }
        public bool PrintToConsole { get; set; }
        public int ProcessorAffinity { get; set; }

        public Benchmark()
        {
            MaxWarmUpIterationCount = BenchmarkSettings.Instance.DefaultMaxWarmUpIterationCount;
            WarmUpIterationCount = BenchmarkSettings.Instance.DefaultWarmUpIterationCount;
            MaxWarmUpError = BenchmarkSettings.Instance.DefaultMaxWarmUpError;
            ResultIterationCount = BenchmarkSettings.Instance.DefaultResultIterationCount;
            PrintToConsole = BenchmarkSettings.Instance.DefaultPrintBenchmarkBodyToConsole;
            ProcessorAffinity = BenchmarkSettings.Instance.DefaultProcessorAffinity;
        }

        public BenchmarkInfo Run(Action action)
        {
            Prepare();

            var info = new BenchmarkInfo();

            if (MaxWarmUpIterationCount > 0)
            {
                if (PrintToConsole)
                    ConsoleHelper.WriteLineHeader("WarmUp:");
                info.WarmUp = Run(action, MaxWarmUpIterationCount, StopWarmUpPredicate);
                ConsoleHelper.NewLine();
            }

            if (PrintToConsole)
                ConsoleHelper.WriteLineHeader("Result:");
            info.Result = Run(action, ResultIterationCount);

            return info;
        }

        private BenchmarkRunList Run(Action action, int iterationCount, Predicate<BenchmarkRunList> stopPredicate = null)
        {
            var runList = new BenchmarkRunList();
            var stopwatch = new Stopwatch();
            for (int i = 0; i < iterationCount; i++)
            {
                stopwatch.Reset();
                stopwatch.Start();
                action();
                stopwatch.Stop();

                var run = new BenchmarkRun(stopwatch);
                runList.Add(run);
                if (PrintToConsole)
                    run.Print();
                if (stopPredicate != null && stopPredicate(runList))
                    break;
            }
            if (PrintToConsole)
                runList.PrintStatistic();
            return runList;
        }

        private bool StopWarmUpPredicate(BenchmarkRunList runList)
        {
            if (runList.Count < WarmUpIterationCount)
                return false;
            var lastRuns = new BenchmarkRunList();
            for (int i = 0; i < WarmUpIterationCount; i++)
                lastRuns.Add(runList[runList.Count - WarmUpIterationCount + i]);
            return lastRuns.Error < MaxWarmUpError;
        }

        #region Prepare

        private static long environmentTickCount;

        public void Prepare()
        {
            environmentTickCount = Environment.TickCount;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(ProcessorAffinity);
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            GCCleanUp();
        }

        private static void GCCleanUp()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #endregion
    }
}