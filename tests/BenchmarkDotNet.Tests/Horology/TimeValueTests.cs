using System;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Horology
{
    public class TimeValueTests
    {
        private static void Check(TimeUnit unit, Func<double, TimeValue> fromMethod, Func<TimeValue, double> toMethod)
        {
            int[] values = { 1, 42, 10000 };
            foreach (int value in values)
            {
                var timeValue = new TimeValue(value, unit);
                AreEqual(timeValue, fromMethod(value));
                AreEqual(toMethod(timeValue), value);
            }
        }

        [Fact]
        public void ConversionTest()
        {
            Check(TimeUnit.Nanosecond, TimeValue.FromNanoseconds, it => it.ToNanoseconds());
            Check(TimeUnit.Microsecond, TimeValue.FromMicroseconds, it => it.ToMicroseconds());
            Check(TimeUnit.Millisecond, TimeValue.FromMilliseconds, it => it.ToMilliseconds());
            Check(TimeUnit.Second, TimeValue.FromSeconds, it => it.ToSeconds());
            Check(TimeUnit.Minute, TimeValue.FromMinutes, it => it.ToMinutes());
            Check(TimeUnit.Hour, TimeValue.FromHours, it => it.ToHours());
            Check(TimeUnit.Day, TimeValue.FromDays, it => it.ToDays());
        }

        [Fact]
        public void OperatorTest()
        {
            AreEqual(TimeValue.Day / TimeValue.Hour, 24);
            AreEqual(TimeValue.Day / 24.0, TimeValue.Hour);
            AreEqual(TimeValue.Day / 24, TimeValue.Hour);
            AreEqual(TimeValue.Hour * 24.0, TimeValue.Day);
            AreEqual(TimeValue.Hour * 24, TimeValue.Day);
            AreEqual(24.0 * TimeValue.Hour, TimeValue.Day);
            AreEqual(24 * TimeValue.Hour, TimeValue.Day);

            AreEqual(TimeValue.Nanosecond * 1000, TimeValue.Microsecond);
            AreEqual(TimeValue.Microsecond * 1000, TimeValue.Millisecond);
            AreEqual(TimeValue.Millisecond * 1000, TimeValue.Second);
            AreEqual(TimeValue.Second * 60, TimeValue.Minute);
            AreEqual(TimeValue.Minute * 60, TimeValue.Hour);
            AreEqual(TimeValue.Hour * 24, TimeValue.Day);

            Assert.True(TimeValue.Minute < TimeValue.Hour);
            Assert.True(TimeValue.Minute <= TimeValue.Hour);
            Assert.True(TimeValue.Minute * 60 <= TimeValue.Hour);
            Assert.True(TimeValue.Hour > TimeValue.Minute);
            Assert.True(TimeValue.Hour >= TimeValue.Minute);
            Assert.True(TimeValue.Hour >= TimeValue.Minute * 60);

            Assert.False(TimeValue.Minute > TimeValue.Hour);
            Assert.False(TimeValue.Minute >= TimeValue.Hour);
            Assert.False(TimeValue.Hour < TimeValue.Minute);
            Assert.False(TimeValue.Hour <= TimeValue.Minute);
        }

        [AssertionMethod]
        private static void AreEqual(TimeValue expected, TimeValue actual) => AreEqual(expected.Nanoseconds, actual.Nanoseconds);

        [AssertionMethod]
        private static void AreEqual(double expected, double actual) => Assert.Equal(expected, actual, 5);
    }
}