using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [StatisticalTestColumn("500us")]
    [StatisticalTestColumn("3%")]
    [SimpleJob(warmupCount: 0, iterationCount: 5)]
    public class IntroStatisticalTesting
    {
        [Benchmark] public void Sleep50() => Thread.Sleep(50);
        [Benchmark] public void Sleep97() => Thread.Sleep(97);
        [Benchmark] public void Sleep99() => Thread.Sleep(99);
        [Benchmark(Baseline = true)] public void Sleep100() => Thread.Sleep(100);
        [Benchmark] public void Sleep101() => Thread.Sleep(101);
        [Benchmark] public void Sleep103() => Thread.Sleep(103);
        [Benchmark] public void Sleep150() => Thread.Sleep(150);
    }
}