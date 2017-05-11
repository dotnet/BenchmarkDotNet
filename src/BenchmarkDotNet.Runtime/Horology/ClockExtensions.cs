using System;
using System.IO;

namespace BenchmarkDotNet.Horology
{
    public static class ClockExtensions
    {
        public static void PrintInfo(this IClock clock, TextWriter textWriter)
        {
            textWriter.WriteLine($"{clock.GetType().Name}");
            textWriter.WriteLine($"  Frequency = {clock.Frequency}");
            textWriter.WriteLine($"  Resolution = {clock.GetResolution().Nanoseconds} ns");
            textWriter.WriteLine($"  Availability = {(clock.IsAvailable ? "Available" : "Not available")}");
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