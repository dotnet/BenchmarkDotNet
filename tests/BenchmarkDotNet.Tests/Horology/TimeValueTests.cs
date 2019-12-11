using System;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Horology
{
    public class TimeValueTests
    {
        private static void Check(TimeUnit unit, Func<double, TimeInterval> fromMethod, Func<TimeInterval, double> toMethod)
        {
            int[] values = { 1, 42, 10000 };
            foreach (int value in values)
            {
                var timeValue = new TimeInterval(value, unit);
                AreEqual(timeValue, fromMethod(value));
                AreEqual(toMethod(timeValue), value);
            }
        }

        [Fact]
        public void ConversionTest()
        {
            Check(TimeUnit.Nanosecond, TimeInterval.FromNanoseconds, it => it.ToNanoseconds());
            Check(TimeUnit.Microsecond, TimeInterval.FromMicroseconds, it => it.ToMicroseconds());
            Check(TimeUnit.Millisecond, TimeInterval.FromMilliseconds, it => it.ToMilliseconds());
            Check(TimeUnit.Second, TimeInterval.FromSeconds, it => it.ToSeconds());
            Check(TimeUnit.Minute, TimeInterval.FromMinutes, it => it.ToMinutes());
            Check(TimeUnit.Hour, TimeInterval.FromHours, it => it.ToHours());
            Check(TimeUnit.Day, TimeInterval.FromDays, it => it.ToDays());
        }

        [Fact]
        public void OperatorTest()
        {
            AreEqual(TimeInterval.Day / TimeInterval.Hour, 24);
            AreEqual(TimeInterval.Day / 24.0, TimeInterval.Hour);
            AreEqual(TimeInterval.Day / 24, TimeInterval.Hour);
            AreEqual(TimeInterval.Hour * 24.0, TimeInterval.Day);
            AreEqual(TimeInterval.Hour * 24, TimeInterval.Day);
            AreEqual(24.0 * TimeInterval.Hour, TimeInterval.Day);
            AreEqual(24 * TimeInterval.Hour, TimeInterval.Day);

            AreEqual(TimeInterval.Nanosecond * 1000, TimeInterval.Microsecond);
            AreEqual(TimeInterval.Microsecond * 1000, TimeInterval.Millisecond);
            AreEqual(TimeInterval.Millisecond * 1000, TimeInterval.Second);
            AreEqual(TimeInterval.Second * 60, TimeInterval.Minute);
            AreEqual(TimeInterval.Minute * 60, TimeInterval.Hour);
            AreEqual(TimeInterval.Hour * 24, TimeInterval.Day);

            Assert.True(TimeInterval.Minute < TimeInterval.Hour);
            Assert.True(TimeInterval.Minute <= TimeInterval.Hour);
            Assert.True(TimeInterval.Minute * 60 <= TimeInterval.Hour);
            Assert.True(TimeInterval.Hour > TimeInterval.Minute);
            Assert.True(TimeInterval.Hour >= TimeInterval.Minute);
            Assert.True(TimeInterval.Hour >= TimeInterval.Minute * 60);

            Assert.False(TimeInterval.Minute > TimeInterval.Hour);
            Assert.False(TimeInterval.Minute >= TimeInterval.Hour);
            Assert.False(TimeInterval.Hour < TimeInterval.Minute);
            Assert.False(TimeInterval.Hour <= TimeInterval.Minute);
        }

        [AssertionMethod]
        private static void AreEqual(TimeInterval expected, TimeInterval actual) => AreEqual(expected.Nanoseconds, actual.Nanoseconds);

        [AssertionMethod]
        private static void AreEqual(double expected, double actual) => Assert.Equal(expected, actual, 5);
    }
}