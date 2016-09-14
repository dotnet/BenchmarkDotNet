using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.TestDriven
{
    public class FastAndDirtyBenchmarkTestRunner : BenchmarkTestRunner
    {
        // See https://perfdotnet.github.io/BenchmarkDotNet/faq.htm
        public override IConfig GetConfig()
        {
            var config = new ManualConfig();
            config.Add(DefaultConfig.Instance);
            config.Add(Job.Default
                .WithLaunchCount(1)     // benchmark process will be launched only once
                .WithIterationTime(100) // 100ms per iteration
                .WithWarmupCount(3)     // 3 warmup iteration
                .WithTargetCount(3)     // 3 target iteration
            );
            return config;
        }
    }
}
