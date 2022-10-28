using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    // Using percentiles for adequate timings representation
    [Config(typeof(Config))]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 4,
        warmupCount: 3, iterationCount: 20, id: "MyJob")]
    public class IntroPercentiles
    {
        // To share between runs.
        // DO NOT do this in production code. The System.Random IS NOT thread safe.
        private static readonly Random Rnd = new Random();

        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(
                    StatisticColumn.P0,
                    StatisticColumn.P25,
                    StatisticColumn.P50,
                    StatisticColumn.P67,
                    StatisticColumn.P80,
                    StatisticColumn.P85,
                    StatisticColumn.P90,
                    StatisticColumn.P95,
                    StatisticColumn.P100);
            }
        }

        [Benchmark(Baseline = true)]
        public void ConstantDelays() => Thread.Sleep(20);

        [Benchmark]
        public void RandomDelays() => Thread.Sleep(10 + (int) (20 * Rnd.NextDouble()));

        [Benchmark]
        public void RareDelays()
        {
            int rndTime = 10;
            // Bigger delays for 15% of the runs
            if (Rnd.NextDouble() > 0.85)
            {
                rndTime += 30;
            }

            Thread.Sleep(rndTime);
        }
    }
}