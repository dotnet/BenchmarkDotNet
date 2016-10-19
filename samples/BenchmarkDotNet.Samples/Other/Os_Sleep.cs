using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Other
{
    [Config(typeof(Config))]
    public class Os_Sleep
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job
                {
                    Run = { LaunchCount = 3, TargetCount = 100 }
                });
            }
        }

        [Benchmark]
        public void Sleep1()
        {
            Thread.Sleep(1);
        }

        [Benchmark]
        public void Sleep10()
        {
            Thread.Sleep(10);
        }

        [Benchmark]
        public void Sleep100()
        {
            Thread.Sleep(100);
        }
    }
}