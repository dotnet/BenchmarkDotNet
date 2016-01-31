using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Framework
{
    [Config(typeof(Config))]
    public class Framework_Stopwatch
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Clr, Job.Mono);
            }
        }    

        [Benchmark]
        public long Latency()
        {
            return Stopwatch.GetTimestamp();
        }

        [Benchmark]
        public long Granularity()
        {
            long lastTimestamp = Stopwatch.GetTimestamp();
            while (Stopwatch.GetTimestamp() == lastTimestamp)
            {
            }
            return lastTimestamp;
        }
    }
}
