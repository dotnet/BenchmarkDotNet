using System;
using BenchmarkDotNet.Horology;
using Xunit;

namespace BenchmarkDotNet.Tests.Horology
{
    public class TimeSpanExtensionsTests
    {
        private void Check(string expected, TimeSpan time)
        {
            Assert.Equal(expected, time.ToFormattedTotalTime());
        }

        [Fact]
        public void Zero() => Check("00:00:00 (0 sec)", TimeSpan.Zero);

        [Fact]
        public void OneSecond() => Check("00:00:01 (1 sec)", TimeSpan.FromSeconds(1));

        [Fact]
        public void OneMinute() => Check("00:01:00 (60 sec)", TimeSpan.FromMinutes(1));

        [Fact]
        public void OneHour() => Check("01:00:00 (3600 sec)", TimeSpan.FromHours(1));

        [Fact]
        public void OneDay() => Check("24:00:00 (86400 sec)", TimeSpan.FromDays(1));

        [Fact]
        public void TwoDays() => Check("48:00:00 (172800 sec)", TimeSpan.FromDays(2));

        [Fact]
        public void Issue240() => Check("00:39:22 (2362 sec)", TimeSpan.FromSeconds(2362));
    }
}