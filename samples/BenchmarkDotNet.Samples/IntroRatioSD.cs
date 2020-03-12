using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Samples
{
    // Don't remove outliers
    [Outliers(OutlierMode.DontRemove)]
    // Skip jitting, pilot, warmup; measure 10 iterations
    [SimpleJob(RunStrategy.Monitoring, targetCount: 10, invocationCount: 1)]
    public class IntroRatioSD
    {
        private int counter;

        [GlobalSetup]
        public void Setup() => counter = 0;

        [Benchmark(Baseline = true)]
        public void Base()
        {
            Thread.Sleep(100);
            if (++counter % 7 == 0)
                Thread.Sleep(5000); // Emulate outlier
        }

        [Benchmark]
        public void Slow() => Thread.Sleep(200);

        [Benchmark]
        public void Fast() => Thread.Sleep(50);
    }
}