using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroColumns
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry.WithTargetCount(10));
                Add(StatisticColumn.StdDev);
                Add(StatisticColumn.Min);
                Add(StatisticColumn.Max);
            }
        }

        private readonly Random random = new Random();

        [Benchmark]
        public void Benchmark()
        {
            Thread.Sleep(random.Next(2) == 0 ? 10 : 50);
        }
    }
}