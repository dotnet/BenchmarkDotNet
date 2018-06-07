using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 0, targetCount: 5, id: "FastJob")]
    public class IntroBaseline
    {
        private readonly Random random = new Random(42);

        [Params(100, 200)]
        public int BaseTime { get; set; }

        [Benchmark(Baseline = true)]
        public void Baseline() => Thread.Sleep(BaseTime);

        [Benchmark]
        public void Slow() => Thread.Sleep(BaseTime * 2);

        [Benchmark]
        public void Fast() => Thread.Sleep(BaseTime / 2);

        [Benchmark]
        public void Unstable() => Thread.Sleep(BaseTime + (int) ((random.NextDouble() - 0.5) * 2 * BaseTime));
    }
}