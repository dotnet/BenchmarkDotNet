using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SingleRunFastConfig : ManualConfig
    {
        public SingleRunFastConfig()
        {
            Add(Job.Dry);
        }
    }

    public class SingleRunMediumConfig : ManualConfig
    {
        public SingleRunMediumConfig()
        {
            Add(Job.Dry.WithTargetCount(5));
        }
    }

    public class ThroughputFastConfig : ManualConfig
    {
        public ThroughputFastConfig()
        {
            Add(Job.Dry.With(RunStrategy.Throughput).WithTargetCount(1));
        }
    }

    public class DiagnoserConfig : ManualConfig
    {
        public DiagnoserConfig()
        {
            // Diagnosers need enough runs to collects the statistics!
            Add(Job.Default.WithLaunchCount(1).WithWarmupCount(5).WithTargetCount(5));
        }
    }
}