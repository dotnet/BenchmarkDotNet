using System.Diagnostics;

namespace BenchmarkDotNet.Reports
{
    internal sealed class BenchmarkRunReport : IBenchmarkRunReport
    {
        public BenchmarkRunReport(Stopwatch stopwatch)
        {
            Ticks = stopwatch.ElapsedTicks;
            Milliseconds = stopwatch.ElapsedMilliseconds;
        }

        public long Ticks { get; set; }
        public long Milliseconds { get; set; }
    }
}