using System;
using System.Diagnostics;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public static class BenchmarkUtils
    {
        public static void SingleRun<T>(Func<T> action, string[] args)
        {
            var settings = BenchmarkSettings.Parse(args);
            var stopwatch = new Stopwatch();
            for (int i = 0; i < settings.WarmupIterationCount; i++)
            {
                Console.Write($"// WarmUp {i + 1}: ");
                stopwatch.Start();
                action();
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds + " ms");
                stopwatch.Reset();
                AutoGcCollect();
            }
            for (int i = 0; i < settings.TargetIterationCount; i++)
            {
                Console.Write($"Target {i + 1}: ");
                stopwatch.Start();
                action();
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds + " ms");
                stopwatch.Reset();
                AutoGcCollect();
            }
        }

        private static void AutoGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}