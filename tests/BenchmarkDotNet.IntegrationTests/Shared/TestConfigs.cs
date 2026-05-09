using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SingleJobConfig : ManualConfig
    {
        public SingleJobConfig(Job job)
        {
            AddJob(job);
        }
    }

    public class SingleRunFastConfig : ManualConfig
    {
        public SingleRunFastConfig()
        {
            AddJob(Job.Dry);
        }
    }

    public class SingleRunMediumConfig : ManualConfig
    {
        public SingleRunMediumConfig()
        {
            AddJob(new Job(Job.Dry) { Run = { IterationCount = 5 } });
        }
    }

    public class ThroughputFastConfig : ManualConfig
    {
        public ThroughputFastConfig()
        {
            AddJob(new Job(Job.Dry) { Run = { RunStrategy = RunStrategy.Throughput, IterationCount = 1 } });
        }
    }

    public class DiagnoserConfig : ManualConfig
    {
        public DiagnoserConfig()
        {
            // Diagnosers need enough runs to collects the statistics!
            AddJob(new Job { Run = { LaunchCount = 1, WarmupCount = 1, IterationCount = 50 } });
        }
    }

    public class SingleRunInProcessConfig : ManualConfig
    {
        public SingleRunInProcessConfig(ITestOutputHelper? output = null, bool addDryJob = true)
        {
            AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddLogger(output != null ? new OutputLogger(output) : ConsoleLogger.Default);

            var job = Job.Dry
                         .WithStrategy(RunStrategy.Monitoring)
                         .WithToolchain(InProcessEmitToolchain.Default);
            AddJob(job);
        }
    }

    public class SingleRunOutOfProcessConfig : ManualConfig
    {
        public SingleRunOutOfProcessConfig(ITestOutputHelper? output = null)
        {
            AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddLogger(output != null ? new OutputLogger(output) : ConsoleLogger.Default);
        }
    }
}