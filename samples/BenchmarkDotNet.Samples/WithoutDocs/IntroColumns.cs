using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 0, targetCount: 10, id: "FastJob")]
    [MinColumn, MaxColumn]
    public class IntroColumns
    {
        private readonly Random random = new Random();

        [Benchmark]
        public void Benchmark() => Thread.Sleep(random.Next(2) == 0 ? 10 : 50);
    }
}