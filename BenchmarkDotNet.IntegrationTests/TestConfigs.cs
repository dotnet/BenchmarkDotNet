using BenchmarkDotNet.Configs;
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
            Add(Job.Dry.With(Mode.Throughput).WithTargetCount(1));
        }
    }
}