using System.Linq;

namespace BenchmarkDotNet.Common
{
    public class TimeUnit
    {
        public string Name { get; }
        public string Description { get; }
        public int NanosecondAmount { get; }

        private TimeUnit(string name, string description, int nanosecondAmount)
        {
            Name = name;
            Description = description;
            NanosecondAmount = nanosecondAmount;
        }

        public static readonly TimeUnit Nanoseconds = new TimeUnit("ns", "Nanoseconds", 1);
        public static readonly TimeUnit Microseconds = new TimeUnit("us", "Microseconds", 1000);
        public static readonly TimeUnit Millisecond = new TimeUnit("ms", "Millisecond", 1000 * 1000);
        public static readonly TimeUnit Second = new TimeUnit("s", "Second", 1000 * 1000 * 1000);
        public static readonly TimeUnit Minute = new TimeUnit("m", "Minute", Second.NanosecondAmount * 60);
        public static readonly TimeUnit Hour = new TimeUnit("h", "Hour", Minute.NanosecondAmount * 60);
        public static readonly TimeUnit Day = new TimeUnit("d", "Day", Hour.NanosecondAmount * 24);
        public static readonly TimeUnit[] All = { Nanoseconds, Microseconds, Millisecond, Second, Minute, Hour, Day };

        /// <summary>
        /// This method chooses the best time unit for representing a set of time measurements. 
        /// </summary>
        /// <param name="values">The list of time measurements in nanoseconds.</param>
        /// <returns>Best time unit.</returns>
        /// <remarks>
        /// The measurements are formatted in such a way that they use the same time unit
        /// the number of decimals so that they are easily comparable and align nicely.
        ///
        /// Example:
        /// Consider we have the following raw input where numbers are durations in nanoseconds:
        ///     Median=597855, StdErr=485;
        ///     Median=7643, StdErr=87;
        ///
        /// When using the formatting function, the output will be like this:
        ///     597.8550 us, 0.0485 us;
        ///       7.6430 us, 0.0087 us;
        /// </remarks>
        public static TimeUnit GetBestTimeUnit(params double[] values)
        {
            if (values.Length == 0)
                return Nanoseconds;
            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            var minValue = values.Min();
            foreach (var timeUnit in All)
            {
                if (minValue < timeUnit.NanosecondAmount * 1000)
                    return timeUnit;
            }
            return All.Last();
        }

        public static double Convert(double value, TimeUnit from, TimeUnit to) => value * @from.NanosecondAmount / (to ?? GetBestTimeUnit(value)).NanosecondAmount;
    }
}