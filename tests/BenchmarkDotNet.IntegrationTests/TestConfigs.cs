using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SingleJobConfig : ManualConfig
    {
        public SingleJobConfig(Job job)
        {
            Add(job);
        }
    }

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
            Add(new Job(Job.Dry) { Run = { IterationCount = 5 } });
        }
    }

    public class ThroughputFastConfig : ManualConfig
    {
        public ThroughputFastConfig()
        {
            Add(new Job(Job.Dry) { Run = { RunStrategy = RunStrategy.Throughput, IterationCount = 1 } });
        }
    }

    public class DiagnoserConfig : ManualConfig
    {
        public DiagnoserConfig()
        {
            // Diagnosers need enough runs to collects the statistics!
            Add(new Job { Run = { LaunchCount = 1, WarmupCount = 1, IterationCount = 50 } });
        }
    }
}