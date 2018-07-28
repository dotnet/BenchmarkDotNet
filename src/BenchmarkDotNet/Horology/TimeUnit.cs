using System.Linq;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public class TimeUnit
    {
        private readonly MultiEncodingString name;
        
        public MultiEncodingString Name => name;
        public string Description { get; }
        public long NanosecondAmount { get; }

        private TimeUnit(MultiEncodingString name, string description, long nanosecondAmount)
        {
            this.name = name;
            Description = description;
            NanosecondAmount = nanosecondAmount;
        }

        public TimeInterval ToInterval(long value = 1) => new TimeInterval(value, this);

        [PublicAPI] public static readonly TimeUnit Nanosecond = new TimeUnit("ns", "Nanosecond", 1);
        [PublicAPI] public static readonly TimeUnit Microsecond = new TimeUnit(new MultiEncodingString("us", "\u03BCs"), "Microsecond", 1000);
        [PublicAPI] public static readonly TimeUnit Millisecond = new TimeUnit("ms", "Millisecond", 1000 * 1000);
        [PublicAPI] public static readonly TimeUnit Second = new TimeUnit("s", "Second", 1000 * 1000 * 1000);
        [PublicAPI] public static readonly TimeUnit Minute = new TimeUnit("m", "Minute", Second.NanosecondAmount * 60);
        [PublicAPI] public static readonly TimeUnit Hour = new TimeUnit("h", "Hour", Minute.NanosecondAmount * 60);
        [PublicAPI] public static readonly TimeUnit Day = new TimeUnit("d", "Day", Hour.NanosecondAmount * 24);
        [PublicAPI] public static readonly TimeUnit[] All = { Nanosecond, Microsecond, Millisecond, Second, Minute, Hour, Day };

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