using System;
using System.Diagnostics;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public static class BenchmarkUtils
    {
        private static volatile object staticValue;

        public static void SingleRun<T>(Func<T> action, string[] args)
        {
            var settings = BenchmarkSettings.Parse(args);
            var stopwatch = new Stopwatch();
            T value = default(T);
            for (int i = 0; i < settings.WarmupIterationCount; i++)
            {
                Console.Write($"// WarmUp {i + 1}: ");
                stopwatch.Start();
                value = action();
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds + " ms");
                stopwatch.Reset();
                AutoGcCollect();
            }
            for (int i = 0; i < settings.TargetIterationCount; i++)
            {
                Console.Write($"Target {i + 1}: ");
                stopwatch.Start();
                value = action();
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds + " ms");
                stopwatch.Reset();
                AutoGcCollect();
            }
            staticValue = value;
            staticValue = null;
        }

        public static void SingleRunVoid(Action action, string[] args)
        {
            var func = new Func<int>(() => { action(); return 0; });
            SingleRun(func, args);
        }

        private static void AutoGcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}