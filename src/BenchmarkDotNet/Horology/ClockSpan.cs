using System;

namespace BenchmarkDotNet.Horology
{
    public struct ClockSpan
    {
        public long StartTimestamp, EndTimestamp;
        public long Frequency;

        public ClockSpan(long startTimestamp, long endTimestamp, long frequency)
        {
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            Frequency = frequency;
        }

        public double GetSeconds() => 1.0 * Math.Max(0, EndTimestamp - StartTimestamp) / Frequency;

        public double GetNanoseconds() => GetSeconds() * TimeUnit.Second.NanosecondAmount;

        public long GetDateTimeTicks() => (long)Math.Round(GetSeconds() * TimeSpan.TicksPerSecond);

        public TimeSpan GetTimeSpan() => new TimeSpan(GetDateTimeTicks());
    }
}