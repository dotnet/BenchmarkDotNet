using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Framework
{
    // This shows that not only is Stopwatch more accurate (granularity) but it's cheaper/quicker to call each time (latency)
    // Inspired by http://shipilev.net/blog/2014/nanotrusting-nanotime/#_latency and http://shipilev.net/blog/2014/nanotrusting-nanotime/#_granularity
    [Config(typeof(Config))]
    public class Framework_StopwatchVsDateTime
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Clr, Job.Mono);
                Add(StatisticColumn.Min);
                Add(new TagColumn("Tool", name => name.Replace(GetMetric(name), "")));
                Add(new TagColumn("Metric", GetMetric));
            }

            private static string GetMetric(string name)
            {
                return name.Contains("Latency") ? "Latency" : "Granularity";
            }
        }


        [Benchmark]
        public long StopwatchLatency()
        {
            return Stopwatch.GetTimestamp();
        }

        [Benchmark]
        public long DateTimeLatency()
        {
            return DateTime.Now.Ticks;
        }

        [Benchmark]
        public long StopwatchGranularity()
        {
            long lastTimestamp = Stopwatch.GetTimestamp();
            while (Stopwatch.GetTimestamp() == lastTimestamp)
            {
            }
            return lastTimestamp;
        }

        [Benchmark]
        public long DateTimeGranularity()
        {
            long lastTicks = DateTime.UtcNow.Ticks;
            while (DateTime.UtcNow.Ticks == lastTicks)
            {
            }
            return lastTicks;
        }
    }
}
