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
            ConsoleHelper.WriteResult("Ticks: {0} ms: {1}",
                ElapsedTicks.ToCultureString().PadLeft(ticksWidth),
                ElapsedMilliseconds.ToCultureString().PadLeft(millisecondsWidth));
            if (!string.IsNullOrEmpty(hint))
                ConsoleHelper.WriteResult(" [{0}]", hint);
            ConsoleHelper.NewLine();
        }
    }
}