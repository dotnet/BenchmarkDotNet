using System;
using System.Diagnostics;
using System.Threading;

namespace BenchmarkDotNet
{
    public class Benchmark
    {
        private const int DefaultMaxWarmUpIterationCount = 30;
        private const int DefaultWarmUpIterationCount = 5;
        private const double DefaultMaxWarpUpError = 0.05;
        private const int DefaultResultIterationCount = 10;
        private const bool DefaultPrintToConsole = true;

        public int MaxWarmUpIterationCount { get; set; }
        public int WarmUpIterationCount { get; set; }
        public double MaxWarpUpError { get; set; }

        public int ResultIterationCount { get; set; }

        public bool PrintToConsole { get; set; }

        public Benchmark()
        {
            MaxWarmUpIterationCount = DefaultMaxWarmUpIterationCount;
            WarmUpIterationCount = DefaultWarmUpIterationCount;
            MaxWarpUpError = DefaultMaxWarpUpError;
            ResultIterationCount = DefaultResultIterationCount;
            PrintToConsole = DefaultPrintToConsole;
        }

        public BenchmarkInfo Run(Action action)
        {
            Prepare();

            var info = new BenchmarkInfo();

            if (PrintToConsole)
                Console.WriteLine("WarmUp:");
            info.WarmUp = Run(action, MaxWarmUpIterationCount, StopWarmUpPredicate);

            if (PrintToConsole)
                Console.WriteLine("\nResult:");
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
            return lastRuns.Error < MaxWarpUpError;
        }

        #region Prepare

        private static long environmentTickCount;

        public static void Prepare()
        {
            environmentTickCount = Environment.TickCount;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1);
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