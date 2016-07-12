using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Samples.Intro
{
    [OrderProvider(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    [DryConfig]
    public class IntroOrderAttr
    {
        [Params(1, 2, 3)]
        public int X { get; set; }

        [Benchmark]
        public void Slow() => Thread.Sleep(X * 100);

        [Benchmark]
        public void Fast() => Thread.Sleep(X * 50);
    }
}