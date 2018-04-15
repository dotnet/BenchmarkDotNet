namespace BenchmarkDotNet.Horology
{
    public static class ClockExtensions
    {
        public static TimeInterval GetResolution(this IClock clock) => clock.Frequency.ToResolution();

        public static StartedClock Start(this IClock clock) => new StartedClock(clock, clock.GetTimestamp());
    }
}