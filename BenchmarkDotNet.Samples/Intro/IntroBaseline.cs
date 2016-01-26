using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroBaseline
    {
        [Benchmark(Baseline = true)]
        public void BaselineMethod()
        {
            Thread.Sleep(100);
        }

        [Benchmark]
        public void Fast()
        {
            Thread.Sleep(50);
        }

        [Benchmark]
        public void Slow()
        {
            Thread.Sleep(150);
        }
    }
}