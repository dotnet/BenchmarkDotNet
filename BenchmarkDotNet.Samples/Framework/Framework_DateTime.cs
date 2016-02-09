using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Framework
{
    [Config(typeof(Config))]
    public class Framework_DateTime
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Clr, Job.Mono);
                Add(new TagColumn("Tool", name => name.Replace(GetMetric(name), "")));
                Add(new TagColumn("Metric", GetMetric));
            }

            private static string GetMetric(string name)
            {
                return name.Contains("Latency") ? "Latency" : "Granularity";
            }
        }

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
