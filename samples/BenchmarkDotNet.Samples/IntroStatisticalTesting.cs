using System.Threading;
using BenchmarkDotNet.Attributes;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace BenchmarkDotNet.Samples
{
    [StatisticalTestColumn(StatisticalTestKind.Welch, ThresholdUnit.Microseconds, 1, true)]
    [StatisticalTestColumn(StatisticalTestKind.MannWhitney, ThresholdUnit.Microseconds, 1, true)]
    [StatisticalTestColumn(StatisticalTestKind.Welch, ThresholdUnit.Ratio, 0.03, true)]
    [StatisticalTestColumn(StatisticalTestKind.MannWhitney, ThresholdUnit.Ratio, 0.03, true)]
    [SimpleJob(warmupCount: 0, targetCount: 5)]
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