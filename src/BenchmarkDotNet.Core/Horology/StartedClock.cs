namespace BenchmarkDotNet.Horology
{
    public struct StartedClock
    {
        private readonly IClock clock;
        private readonly long startTimestamp;

        public StartedClock(IClock clock, long startTimestamp)
        {
            this.clock = clock;
            this.startTimestamp = startTimestamp;
        }

        public ClockSpan GetElapsed() => new ClockSpan(startTimestamp, clock.GetTimestamp(), clock.Frequency);
    }
}