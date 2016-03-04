using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    [DryConfig]
    public class IntroBaseline
    {
        [Params(100, 200)]
        public int BaselineTime { get; set; }

        [Benchmark(Baseline = true)]
        public void BaselineMethod()
        {
            Thread.Sleep(BaselineTime);
        }

        [Benchmark]
        public void Fast()
        {
            Thread.Sleep(BaselineTime / 2);
        }

        [Benchmark]
        public void Slow()
        {
            Thread.Sleep(BaselineTime * 2);
        }
    }
}