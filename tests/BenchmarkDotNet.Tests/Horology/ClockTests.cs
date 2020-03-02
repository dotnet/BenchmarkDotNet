using System.Linq;
using System.Threading;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Horology
{
    public class ClockTests
    {
        private readonly ITestOutputHelper output;

        public ClockTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void SpanTest()
        {
            var span = new ClockSpan(0, 1000, Frequency.Hz * 500);
            Assert.Equal(2, span.GetSeconds(), 5);
            Assert.Equal(2000000000, span.GetNanoseconds(), 2);
            Assert.Equal(20000000L, span.GetDateTimeTicks());
            Assert.Equal(2, span.GetTimeSpan().TotalSeconds, 5);
            Assert.Equal(2, span.GetTimeValue().ToSeconds(), 5);
        }

        [Fact]
        public void MeasureTest()
        {
            var clocks = new[]
            {
                Chronometer.Stopwatch,
                Chronometer.DateTime,
                Chronometer.WindowsClock,
                Chronometer.BestClock
            }.Where(clock => clock.IsAvailable).ToList();

            var startedClocks = clocks.Select(clock => clock.Start()).ToList();
            Thread.Sleep(16);
            var spans = startedClocks.Select(startedClock => startedClock.GetElapsed()).ToList();
            for (int i = 0; i < clocks.Count; i++)
            {
                output.WriteLine(clocks[i].Title + ": " + TimeInterval.FromSeconds(spans[i].GetSeconds()).ToString(TimeUnit.Second, TestCultureInfo.Instance));
                var interval = spans[i].GetTimeValue();
                Assert.True(interval > TimeInterval.Millisecond);
                Assert.True(interval < TimeInterval.Hour);
            }
        }

        [Fact]
        public void ChronometerTest()
        {
            var clock = Chronometer.BestClock;
            output.WriteLine($"Chronometer.BestClock = {clock.Title} ({clock.Frequency.Hertz} Hz)");
            output.WriteLine("-----");

            long chronometerTimestamp1 = Chronometer.GetTimestamp();
            long clockTimestamp1 = clock.GetTimestamp();
            long clockTimestamp2 = clock.GetTimestamp();
            long chronometerTimestamp2 = Chronometer.GetTimestamp();
            output.WriteLine($"chronometerTimestamp1 = {chronometerTimestamp1}");
            output.WriteLine($"clockTimestamp1       = {clockTimestamp1}");
            output.WriteLine($"clockTimestamp2       = {clockTimestamp2}");
            output.WriteLine($"chronometerTimestamp2 = {chronometerTimestamp2}");
            output.WriteLine("-----");
            Assert.True(chronometerTimestamp2 - chronometerTimestamp1 >= clockTimestamp2 - clockTimestamp1);

            var chronometerStared = Chronometer.Start();
            var clockStarted = clock.Start();
            var clockElapsed = clockStarted.GetElapsed();
            var chronometerElapsed = chronometerStared.GetElapsed();
            output.WriteLine($"chronometerStared                    = {chronometerStared}");
            output.WriteLine($"clockStarted                         = {clockStarted}");
            output.WriteLine($"clockElapsed                         = {clockElapsed}");
            output.WriteLine($"chronometerElapsed                   = {chronometerElapsed}");
            output.WriteLine($"chronometerElapsed.GetTimeValue()    = {chronometerElapsed.GetTimeValue()}");
            output.WriteLine($"clockElapsed.GetTimeValue()          = {clockElapsed.GetTimeValue()}");
            output.WriteLine("-----");
            Assert.True(chronometerElapsed.GetTimeValue() >= clockElapsed.GetTimeValue());

            output.WriteLine($"Chronometer.GetResolution().Nanoseconds = {Chronometer.GetResolution().Nanoseconds}");
            output.WriteLine($"clock.GetResolution().Nanoseconds       = {clock.GetResolution().Nanoseconds}");
            Assert.Equal(Chronometer.GetResolution().Nanoseconds, clock.GetResolution().Nanoseconds, 5);
        }
    }
}