using System;
using System.Diagnostics;
using System.Threading;

namespace BenchmarkDotNet
{
    public class Benchmark
    {
        private const int DefaultWarmUpIterationCount = 20;
        private const int DefaultResultIterationCount = 20;
        private const bool DefaultPrintToConsole = true;

        public int WarmUpIterationCount { get; set; }
        public int ResultIterationCount { get; set; }
        public bool PrintToConsole { get; set; }

        public Benchmark()
        {
            WarmUpIterationCount = DefaultWarmUpIterationCount;
            ResultIterationCount = DefaultResultIterationCount;
            PrintToConsole = DefaultPrintToConsole;
        }

        public BenchmarkInfo Run(Action action)
        {
            Prepare();

            var info = new BenchmarkInfo();

            if (PrintToConsole)
                Console.WriteLine("WarmUp:");
            info.WarmUp = Run(action, WarmUpIterationCount);

            if (PrintToConsole)
                Console.WriteLine("\nResult:");
            info.Result = Run(action, ResultIterationCount);

            return info;
        }
     
        private BenchmarkRunList Run(Action action, int iterationCount)
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
            }
            if (PrintToConsole)
                runList.PrintStatistic();
            return runList;
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