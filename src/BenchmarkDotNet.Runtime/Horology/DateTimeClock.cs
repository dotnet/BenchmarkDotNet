using System;

namespace BenchmarkDotNet.Horology
{
    public class DateTimeClock : IClock
    {
        private const long TicksPerSecond = (long)10 * 1000 * 1000;

        public bool IsAvailable => true;
        public Frequency Frequency => new Frequency(TicksPerSecond);
        public long GetTimestamp() => DateTime.UtcNow.Ticks;
    }
}