using System;
using System.Linq;

namespace BenchmarkDotNet.Horology
{
    public static class ClockExtensions
    {
        public static void PrintInfo(this IClock clock)
        {
            Console.WriteLine($"{clock.GetType().Name}");
            Console.WriteLine($"  Frequency = {clock.Frequency}");
            Console.WriteLine($"  Resolution = {clock.GetResolution(TimeUnit.Nanoseconds)} ns");
            Console.WriteLine($"  Availability = {(clock.IsAvailable ? "Available" : "Not available")}");
        }

        public static double GetResolution(this IClock clock, TimeUnit timeUnit = null)
        {
            return TimeUnit.Convert(1.0 / clock.Frequency, TimeUnit.Second, timeUnit ?? TimeUnit.Nanoseconds);
        }

        public static StartedClock Start(this IClock clock)
        {
            return new StartedClock(clock, clock.GetTimestamp());
        }
    }
}