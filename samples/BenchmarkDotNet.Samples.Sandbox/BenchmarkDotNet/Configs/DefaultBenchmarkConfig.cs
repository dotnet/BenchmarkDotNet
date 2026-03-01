using BenchmarkDotNet.Jobs;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet;

public class DefaultBenchmarkConfig : BaseBenchmarkConfig
{
    public DefaultBenchmarkConfig() : base()
    {
        // Configure base job config
        var baseJobConfig = GetBaseJobConfig();

        // Create benchmark job.
        var job = baseJobConfig.WithId($"Default({RuntimeInformation.FrameworkDescription})");

        // Add job.
        AddJob(job);

        // Configure additional settings.
        AddConfigurations();
    }
}
