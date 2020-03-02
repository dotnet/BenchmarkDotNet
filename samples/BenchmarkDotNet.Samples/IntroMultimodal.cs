using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [MValueColumn]
    [SimpleJob(RunStrategy.Throughput, 1, 0, -1, 1, "MyJob")]
    public class IntroMultimodal
    {
        private readonly Random rnd = new Random(42);

        private void Multimodal(int n)
            => Thread.Sleep((rnd.Next(n) + 1) * 100);

        [Benchmark] public void Unimodal() => Multimodal(1);
        [Benchmark] public void Bimodal() => Multimodal(2);
        [Benchmark] public void Trimodal() => Multimodal(3);
        [Benchmark] public void Quadrimodal() => Multimodal(4);
    }
}