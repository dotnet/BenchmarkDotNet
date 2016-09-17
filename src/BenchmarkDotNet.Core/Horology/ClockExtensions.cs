using System;

namespace BenchmarkDotNet.Horology
{
    public static class ClockExtensions
    {
        public static void PrintInfo(this IClock clock)
        {
            Console.WriteLine($"{clock.GetType().Name}");
            Console.WriteLine($"  Frequency = {clock.Frequency}");
            Console.WriteLine($"  Resolution = {clock.GetResolution().Nanoseconds} ns");
            Console.WriteLine($"  Availability = {(clock.IsAvailable ? "Available" : "Not available")}");
        }

        public static TimeInterval GetResolution(this IClock clock)
        {
            return clock.Frequency.ToResolution();
        }

        public static StartedClock Start(this IClock clock)
        {
            return new StartedClock(clock, clock.GetTimestamp());
        }
    }
}