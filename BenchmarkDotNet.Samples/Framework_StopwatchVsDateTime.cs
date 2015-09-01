using System;
using System.Diagnostics;

namespace BenchmarkDotNet.Samples
{
    // This shows that not only is Stopwatch more accurate (granularity) but it's cheaper/quicker to call each time (latency)
    // Inspired by http://shipilev.net/blog/2014/nanotrusting-nanotime/#_latency and http://shipilev.net/blog/2014/nanotrusting-nanotime/#_granularity
    public class Framework_StopwatchVsDateTime
    {
        [Benchmark]
        public long StopwatchLatency()
        {
            return Stopwatch.GetTimestamp();
        }

        [Benchmark]
        public long StopwatchGranularity()
        {
            // Keep calling Stopwatch.GetTimestamp() till we get a different/new value
            long current, lastValue = Stopwatch.GetTimestamp();
            do
            {
                current = Stopwatch.GetTimestamp();
            } while (current == lastValue);
            lastValue = current;
            return current;
        }

        [Benchmark]
        public long DateTimeLatency()
        {
            return DateTime.Now.Ticks;
        }

        [Benchmark]
        public long DateTimeGranularity()
        {
            // Keep calling DateTime.Now.Ticks till we get a different/new value
            long current, lastValue = DateTime.Now.Ticks;
            do
            {
                current = DateTime.Now.Ticks;
            } while (current == lastValue);
            lastValue = current;
            return current;
        }
    }
}
