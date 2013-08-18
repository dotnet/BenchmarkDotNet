using System;
using System.Diagnostics;

namespace BenchmarkDotNet
{
    public class BenchmarkRun
    {
        public BenchmarkRun(Stopwatch stopwatch)
        {
            ElapsedTicks = stopwatch.ElapsedTicks;
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        public long ElapsedTicks { get; set; }
        public long ElapsedMilliseconds { get; set; }

        public void Print(int ticksWidth = 0, int millisecondsWidth = 0, string hint = "")
        {
            Console.Write("Ticks: {0} ms: {1}", 
                ElapsedTicks.ToString().PadLeft(ticksWidth), 
                ElapsedMilliseconds.ToString().PadLeft(millisecondsWidth));
            if (!string.IsNullOrEmpty(hint))
                Console.Write(" [{0}]", hint);
            Console.WriteLine();
        }
    }
}