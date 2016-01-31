namespace BenchmarkDotNet.Horology
{
    public struct StartedClock
    {
        public IClock Clock;
        public long StartTimestamp;

        public StartedClock(IClock clock, long startTimestamp)
        {
            Clock = clock;
            StartTimestamp = startTimestamp;
        }

        public ClockSpan Stop()
        {
            return new ClockSpan(StartTimestamp, Clock.GetTimestamp(), Clock.Frequency);
        }
    }
}