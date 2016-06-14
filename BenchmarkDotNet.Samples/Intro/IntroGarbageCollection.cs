using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroGarbageCollection
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = true }));
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = false }));
            }
        }

        [Benchmark(Description = "new byte[1MB]")]
        public byte[] Allocate()
        {
            return new byte[1000000];
        }
    }
}