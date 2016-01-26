using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroJobsFull
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.LegacyX86.With(Mode.SingleRun).WithProcessCount(1).WithWarmupCount(1).WithTargetCount(3));
            }
        }

        [Benchmark(Baseline = true)]
        public void Sleep()
        {
            Thread.Sleep(100);
        }

        [Benchmark]
        public void Sleep50()
        {
            Thread.Sleep(50);
        }
    }
}