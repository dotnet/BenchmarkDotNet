using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroOutliers
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var jobBase = Job.Default.WithWarmupCount(0).WithIterationCount(10).WithInvocationCount(1).WithUnrollFactor(1);
                Add(jobBase.WithOutlierMode(OutlierMode.DontRemove).WithId("DontRemoveOutliers"));
                Add(jobBase.WithOutlierMode(OutlierMode.RemoveUpper).WithId("RemoveUpperOutliers"));
            }
        }

        private int counter;

        [Benchmark]
        public void Foo()
        {
            counter++;
            int noise = counter % 10 == 0 ? 500 : 0;
            Thread.Sleep(100 + noise);
        }
    }
}