using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroBaseline
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default.WithLaunchCount(0).WithWarmupCount(0).WithTargetCount(5));
            }
        }

        private readonly Random random = new Random(42);

        [Params(100, 200)]
        public int BaselineTime { get; set; }

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            Thread.Sleep(BaselineTime);
        }

        [Benchmark]
        public void Slow()
        {
            Thread.Sleep(BaselineTime * 2);
        }

        [Benchmark]
        public void Fast()
        {
            Thread.Sleep(BaselineTime / 2);
        }

        [Benchmark]
        public void Unstable()
        {
            var diff = (int)((random.NextDouble() - 0.5) * 2 * BaselineTime);
            Thread.Sleep(BaselineTime + diff);
        }
    }
}