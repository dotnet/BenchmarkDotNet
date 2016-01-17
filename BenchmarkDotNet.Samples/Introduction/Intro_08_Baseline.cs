using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    [BenchmarkTask(1, BenchmarkMode.SingleRun, targetIterationCount: 1, warmupIterationCount: 0)]
    public class Intro_08_Baseline
    {
        [Benchmark]
        public void Time50()
        {
            Thread.Sleep(50);
        }

        [Benchmark(Baseline = true)]
        public void Time100()
        {
            Thread.Sleep(100);
        }

        [Benchmark]
        public void Time150()
        {
            Thread.Sleep(150);
        }
    }
}