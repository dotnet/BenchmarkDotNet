using System;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Uap.Framework
{
    public class Framework_DateTime
    {
        [Benchmark]
        public long UtcNowLatency()
        {
            return DateTime.UtcNow.Ticks;
        }

        [Benchmark]
        public long NowLatency()
        {
            return DateTime.Now.Ticks;
        }

        [Benchmark]
        public long UtcNowGranularity()
        {
            long lastTicks = DateTime.UtcNow.Ticks;
            while (DateTime.UtcNow.Ticks == lastTicks)
            {
            }
            return lastTicks;
        }

        [Benchmark]
        public long NowGranularity()
        {
            long lastTicks = DateTime.Now.Ticks;
            while (DateTime.Now.Ticks == lastTicks)
            {
            }
            return lastTicks;
        }
    }
}
