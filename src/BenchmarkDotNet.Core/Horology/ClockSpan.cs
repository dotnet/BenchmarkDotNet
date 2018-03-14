using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public struct ClockSpan
    {
        private readonly long startTimestamp, endTimestamp;
        private readonly Frequency frequency;

        public ClockSpan(long startTimestamp, long endTimestamp, Frequency frequency)
        {
            this.startTimestamp = startTimestamp;
            this.endTimestamp = endTimestamp;
            this.frequency = frequency;
        }

        [Pure] public double GetSeconds() => 1.0 * Math.Max(0, endTimestamp - startTimestamp) / frequency;
        [Pure] public double GetNanoseconds() => GetSeconds() * TimeUnit.Second.NanosecondAmount;
        [Pure] public long GetDateTimeTicks() => (long) Math.Round(GetSeconds() * TimeSpan.TicksPerSecond);
        [Pure] public TimeSpan GetTimeSpan() => new TimeSpan(GetDateTimeTicks());
        [Pure] public TimeInterval GetTimeInterval() => new TimeInterval(GetNanoseconds());

        public override string ToString() => $"ClockSpan({startTimestamp} ticks, {endTimestamp} ticks, {frequency.Hertz} Hz)";
    }
}