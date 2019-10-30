using System.Globalization;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public struct TimeValue
    {
        public double Nanoseconds { get; }

        public TimeValue(double nanoseconds) => Nanoseconds = nanoseconds;

        public TimeValue(double value, TimeUnit unit) : this(value * unit.NanosecondAmount) { }

        public static readonly TimeValue Nanosecond = TimeUnit.Nanosecond.ToInterval();
        public static readonly TimeValue Microsecond = TimeUnit.Microsecond.ToInterval();
        public static readonly TimeValue Millisecond = TimeUnit.Millisecond.ToInterval();
        public static readonly TimeValue Second = TimeUnit.Second.ToInterval();
        public static readonly TimeValue Minute = TimeUnit.Minute.ToInterval();
        public static readonly TimeValue Hour = TimeUnit.Hour.ToInterval();
        public static readonly TimeValue Day = TimeUnit.Day.ToInterval();

        [Pure] public Frequency ToFrequency() => new Frequency(Second / this);

        [Pure] public double ToNanoseconds() => this / Nanosecond;
        [Pure] public double ToMicroseconds() => this / Microsecond;
        [Pure] public double ToMilliseconds() => this / Millisecond;
        [Pure] public double ToSeconds() => this / Second;
        [Pure] public double ToMinutes() => this / Minute;
        [Pure] public double ToHours() => this / Hour;
        [Pure] public double ToDays() => this / Day;

        [Pure] public static TimeValue FromNanoseconds(double value) => Nanosecond * value;
        [Pure] public static TimeValue FromMicroseconds(double value) => Microsecond * value;
        [Pure] public static TimeValue FromMilliseconds(double value) => Millisecond * value;
        [Pure] public static TimeValue FromSeconds(double value) => Second * value;
        [Pure] public static TimeValue FromMinutes(double value) => Minute * value;
        [Pure] public static TimeValue FromHours(double value) => Hour * value;
        [Pure] public static TimeValue FromDays(double value) => Day * value;

        [Pure] public static double operator /(TimeValue a, TimeValue b) => 1.0 * a.Nanoseconds / b.Nanoseconds;
        [Pure] public static TimeValue operator /(TimeValue a, double k) => new TimeValue(a.Nanoseconds / k);
        [Pure] public static TimeValue operator /(TimeValue a, int k) => new TimeValue(a.Nanoseconds / k);
        [Pure] public static TimeValue operator *(TimeValue a, double k) => new TimeValue(a.Nanoseconds * k);
        [Pure] public static TimeValue operator *(TimeValue a, int k) => new TimeValue(a.Nanoseconds * k);
        [Pure] public static TimeValue operator *(double k, TimeValue a) => new TimeValue(a.Nanoseconds * k);
        [Pure] public static TimeValue operator *(int k, TimeValue a) => new TimeValue(a.Nanoseconds * k);
        [Pure] public static bool operator <(TimeValue a, TimeValue b) => a.Nanoseconds < b.Nanoseconds;
        [Pure] public static bool operator >(TimeValue a, TimeValue b) => a.Nanoseconds > b.Nanoseconds;
        [Pure] public static bool operator <=(TimeValue a, TimeValue b) => a.Nanoseconds <= b.Nanoseconds;
        [Pure] public static bool operator >=(TimeValue a, TimeValue b) => a.Nanoseconds >= b.Nanoseconds;

        [Pure, NotNull]
        public string ToString(
            [CanBeNull] CultureInfo cultureInfo,
            [CanBeNull] string format = "N4",
            [CanBeNull] UnitPresentation unitPresentation = null)
        {
            return ToString(null, cultureInfo, format, unitPresentation);
        }

        [Pure, NotNull]
        public string ToString(
            [CanBeNull] TimeUnit timeUnit,
            [CanBeNull] CultureInfo cultureInfo,
            [CanBeNull] string format = "N4",
            [CanBeNull] UnitPresentation unitPresentation = null)
        {
            timeUnit = timeUnit ?? TimeUnit.GetBestTimeUnit(Nanoseconds);
            cultureInfo = cultureInfo ?? DefaultCultureInfo.Instance;
            format = format ?? "N4";
            unitPresentation = unitPresentation ?? UnitPresentation.Default;
            double unitValue = TimeUnit.Convert(Nanoseconds, TimeUnit.Nanosecond, timeUnit);
            if (unitPresentation.IsVisible)
            {
                string unitName = timeUnit.Name.PadLeft(unitPresentation.MinUnitWidth);
                return $"{unitValue.ToString(format, cultureInfo)} {unitName}";
            }

            return unitValue.ToString(format, cultureInfo);
        }

        [Pure] public override string ToString() => ToString(DefaultCultureInfo.Instance);
    }
}