using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet;

public class DebugBenchmarkConfig : BaseBenchmarkConfig
{
    public DebugBenchmarkConfig()
    {
        // Configure base job config
        var baseJobConfig = GetBaseJobConfig();

        // Add benchmark job.
        AddJob(baseJobConfig.WithCustomBuildConfiguration("Debug")
                            .WithWarmupCount(1)
                            .WithStrategy(RunStrategy.Monitoring)
                            .WithId($"Debug({RuntimeInformation.FrameworkDescription})"));

        // Set DebugConfig comatible option
        WithOptions(ConfigOptions.KeepBenchmarkFiles);

        // Configure additional settings.
        AddConfigurations();
    }
}
