using System;
using System.Threading;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    // Using percentiles for adequate timings representation
    [Config(typeof(Config))]
    public class IntroPercentiles
    {
        // To share between runs.
        // DO NOT do this in production code. The System.Random IS NOT threadsafe.
        private static readonly Random rnd = new Random();

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job
                {
                    Mode = Mode.SingleRun,
                    LaunchCount = 4,
                    WarmupCount = 3,
                    TargetCount = 20
                });
                Add(
                    StatisticColumn.P0,
                    StatisticColumn.P25,
                    StatisticColumn.P50,
                    StatisticColumn.P67,
                    StatisticColumn.P80,
                    StatisticColumn.P85,
                    StatisticColumn.P90,
                    StatisticColumn.P95,
                    StatisticColumn.P100,
                    BaselineDiffColumn.Scaled50,
                    BaselineDiffColumn.Scaled85,
                    BaselineDiffColumn.Scaled95);
            }
        }

        [Benchmark(Baseline = true)]
        public void ConstantDelays()
        {
            Thread.Sleep(20);
        }

        [Benchmark]
        public void RandomDelays()
        {
            Thread.Sleep(10 + (int)(20 * rnd.NextDouble()));
        }

        [Benchmark]
        public void RareDelays()
        {
            var rndTime = 10;
            // Bigger delays for 15% of the runs
            if (rnd.NextDouble() > 0.85)
            {
                rndTime += 30;
            }
            Thread.Sleep(rndTime);
        }
    }
}