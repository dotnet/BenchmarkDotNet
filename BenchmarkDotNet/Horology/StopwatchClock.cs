using System.Diagnostics;

namespace BenchmarkDotNet.Horology
{
    public class StopwatchClock : IClock
    {
        public bool IsAvailable => true;
        public long Frequency => Stopwatch.Frequency;
        public long GetTimestamp() => Stopwatch.GetTimestamp();
    }
}