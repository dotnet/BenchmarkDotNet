using System.Diagnostics;

namespace BenchmarkDotNet.Horology
{
    public class StopwatchClock : IClock
    {
        public bool IsAvailable => true;
        public Frequency Frequency => new Frequency(Stopwatch.Frequency);
        public long GetTimestamp() => Stopwatch.GetTimestamp();
    }
}