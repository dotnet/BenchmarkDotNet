﻿using System.Linq;

namespace BenchmarkDotNet.Horology
{
    public class TimeUnit
    {
        public string Name { get; }
        public string Description { get; }
        public long NanosecondAmount { get; }

        private TimeUnit(string name, string description, long nanosecondAmount)
        {
            Name = name;
            Description = description;
            NanosecondAmount = nanosecondAmount;
        }

        public TimeInterval ToInterval(long value = 1) => new TimeInterval(value, this);

        public static readonly TimeUnit Nanosecond = new TimeUnit("ns", "Nanosecond", 1);
        public static readonly TimeUnit Microsecond = new TimeUnit("us", "Microsecond", 1000);
        public static readonly TimeUnit Millisecond = new TimeUnit("ms", "Millisecond", 1000 * 1000);
        public static readonly TimeUnit Second = new TimeUnit("s", "Second", 1000 * 1000 * 1000);
        public static readonly TimeUnit Minute = new TimeUnit("m", "Minute", Second.NanosecondAmount * 60);
        public static readonly TimeUnit Hour = new TimeUnit("h", "Hour", Minute.NanosecondAmount * 60);
        public static readonly TimeUnit Day = new TimeUnit("d", "Day", Hour.NanosecondAmount * 24);
        public static readonly TimeUnit[] All = { Nanosecond, Microsecond, Millisecond, Second, Minute, Hour, Day };

        /// <summary>
        /// This method chooses the best time unit for representing a set of time measurements. 
        /// </summary>
        /// <param name="values">The list of time measurements in nanoseconds.</param>
        /// <returns>Best time unit.</returns>
        public static TimeUnit GetBestTimeUnit(params double[] values)
        {
            if (values.Length == 0)
                return Nanosecond;
            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            double minValue = values.Min();
            foreach (var timeUnit in All)
                if (minValue < timeUnit.NanosecondAmount * 1000)
                    return timeUnit;
            return All.Last();
        }

        public static double Convert(double value, TimeUnit from, TimeUnit to) =>
            value * from.NanosecondAmount / (to ?? GetBestTimeUnit(value)).NanosecondAmount;
    }
}