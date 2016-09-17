using JetBrains.Annotations;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Horology
{
    public struct TimeInterval
    {
        public double Nanoseconds { get; }

        public TimeInterval(double nanoseconds)
        {
            Nanoseconds = nanoseconds;
        }

        public TimeInterval(double value, TimeUnit unit) : this(value * unit.NanosecondAmount)
        {
        }

        [Pure]
        public Frequency ToFrequency() => new Frequency(Second / this);

        [Pure]
        public double ToNanoseconds() => this / Nanosecond;

        [Pure]
        public double ToMicroseconds() => this / Microsecond;

        [Pure]
        public double ToMilliseconds() => this / Millisecond;

        [Pure]
        public double ToSeconds() => this / Second;

        [Pure]
        public double ToMinutes() => this / Minute;

        [Pure]
        public double ToHours() => this / Hour;

        [Pure]
        public double ToDays() => this / Day;

        public override string ToString() => Nanoseconds.ToTimeStr();

        public static readonly TimeInterval Nanosecond = TimeUnit.Nanosecond.ToInterval();
        public static readonly TimeInterval Microsecond = TimeUnit.Microsecond.ToInterval();
        public static readonly TimeInterval Millisecond = TimeUnit.Millisecond.ToInterval();
        public static readonly TimeInterval Second = TimeUnit.Second.ToInterval();
        public static readonly TimeInterval Minute = TimeUnit.Minute.ToInterval();
        public static readonly TimeInterval Hour = TimeUnit.Hour.ToInterval();
        public static readonly TimeInterval Day = TimeUnit.Day.ToInterval();

        public static double operator /(TimeInterval a, TimeInterval b) => 1.0 * a.Nanoseconds / b.Nanoseconds;
        public static TimeInterval operator *(TimeInterval a, double k) => new TimeInterval(a.Nanoseconds * k);
        public static TimeInterval operator *(double k, TimeInterval a) => new TimeInterval(a.Nanoseconds * k);
        public static TimeInterval operator /(TimeInterval a, double k) => new TimeInterval(a.Nanoseconds / k);
    }
}