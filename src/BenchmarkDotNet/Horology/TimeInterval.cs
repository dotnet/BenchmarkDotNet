﻿using System.Globalization;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Horology
{
    public struct TimeInterval
    {
        public double Nanoseconds { get; }

        public TimeInterval(double nanoseconds) => Nanoseconds = nanoseconds;

        public TimeInterval(double value, TimeUnit unit) : this(value * unit.NanosecondAmount) { }

        public static readonly TimeInterval Nanosecond = TimeUnit.Nanosecond.ToInterval();
        public static readonly TimeInterval Microsecond = TimeUnit.Microsecond.ToInterval();
        public static readonly TimeInterval Millisecond = TimeUnit.Millisecond.ToInterval();
        public static readonly TimeInterval Second = TimeUnit.Second.ToInterval();
        public static readonly TimeInterval Minute = TimeUnit.Minute.ToInterval();
        public static readonly TimeInterval Hour = TimeUnit.Hour.ToInterval();
        public static readonly TimeInterval Day = TimeUnit.Day.ToInterval();

        [Pure] public Frequency ToFrequency() => new Frequency(Second / this);

        [Pure] public double ToNanoseconds() => this / Nanosecond;
        [Pure] public double ToMicroseconds() => this / Microsecond;
        [Pure] public double ToMilliseconds() => this / Millisecond;
        [Pure] public double ToSeconds() => this / Second;
        [Pure] public double ToMinutes() => this / Minute;
        [Pure] public double ToHours() => this / Hour;
        [Pure] public double ToDays() => this / Day;

        [Pure] public static TimeInterval FromNanoseconds(double value) => Nanosecond * value;
        [Pure] public static TimeInterval FromMicroseconds(double value) => Microsecond * value;
        [Pure] public static TimeInterval FromMilliseconds(double value) => Millisecond * value;
        [Pure] public static TimeInterval FromSeconds(double value) => Second * value;
        [Pure] public static TimeInterval FromMinutes(double value) => Minute * value;
        [Pure] public static TimeInterval FromHours(double value) => Hour * value;
        [Pure] public static TimeInterval FromDays(double value) => Day * value;

        [Pure] public static double operator /(TimeInterval a, TimeInterval b) => 1.0 * a.Nanoseconds / b.Nanoseconds;
        [Pure] public static TimeInterval operator /(TimeInterval a, double k) => new TimeInterval(a.Nanoseconds / k);
        [Pure] public static TimeInterval operator /(TimeInterval a, int k) => new TimeInterval(a.Nanoseconds / k);
        [Pure] public static TimeInterval operator *(TimeInterval a, double k) => new TimeInterval(a.Nanoseconds * k);
        [Pure] public static TimeInterval operator *(TimeInterval a, int k) => new TimeInterval(a.Nanoseconds * k);
        [Pure] public static TimeInterval operator *(double k, TimeInterval a) => new TimeInterval(a.Nanoseconds * k);
        [Pure] public static TimeInterval operator *(int k, TimeInterval a) => new TimeInterval(a.Nanoseconds * k);
        [Pure] public static bool operator <(TimeInterval a, TimeInterval b) => a.Nanoseconds < b.Nanoseconds;
        [Pure] public static bool operator >(TimeInterval a, TimeInterval b) => a.Nanoseconds > b.Nanoseconds;
        [Pure] public static bool operator <=(TimeInterval a, TimeInterval b) => a.Nanoseconds <= b.Nanoseconds;
        [Pure] public static bool operator >=(TimeInterval a, TimeInterval b) => a.Nanoseconds >= b.Nanoseconds;

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