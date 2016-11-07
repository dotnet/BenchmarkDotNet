using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroOutliers
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var jobBase = Job.Default.WithWarmupCount(0).WithTargetCount(10).WithInvocationCount(1).WithUnrollFactor(1);
                Add(jobBase.WithRemoveOutliers(false).WithId("DontRemoveOutliers"));
                Add(jobBase.WithRemoveOutliers(true).WithId("RemoveOutliers"));
            }
        }

        private int counter = 0;

        [Benchmark]
        public void Foo()
        {
            counter++;
            int noise = counter % 10 == 0 ? 500 : 0;
            Thread.Sleep(100 + noise);
        }
    }
}