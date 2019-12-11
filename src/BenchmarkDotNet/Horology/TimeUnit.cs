﻿using System;
using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public class TimeUnit : IEquatable<TimeUnit>
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

        [PublicAPI] public static readonly TimeUnit Nanosecond = new TimeUnit("ns", "Nanosecond", 1);
        [PublicAPI] public static readonly TimeUnit Microsecond = new TimeUnit("\u03BCs", "Microsecond", 1000);
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

        public bool Equals(TimeUnit other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(Name, other.Name) && string.Equals(Description, other.Description) && NanosecondAmount == other.NanosecondAmount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((TimeUnit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NanosecondAmount.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TimeUnit left, TimeUnit right) => Equals(left, right);

        public static bool operator !=(TimeUnit left, TimeUnit right) => !Equals(left, right);
    }
}