using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet;

public class DebugInProcessBenchmarkConfig : BaseBenchmarkConfig
{
    public DebugInProcessBenchmarkConfig() : base()
    {
        // Configure base job config
        var baseJobConfig = GetBaseJobConfig();

        // Add benchmark job.
        AddJob(baseJobConfig.WithToolchain(InProcessEmitToolchain.Default)
                            .WithWarmupCount(1)
                            .WithStrategy(RunStrategy.Monitoring)
                            .WithId($"DebugInProcess({RuntimeInformation.FrameworkDescription})"));

        // Set DebugConfig comatible option
        WithOptions(ConfigOptions.KeepBenchmarkFiles);

        // Configure additional settings.
        AddConfigurations();
    }

    protected override void AddAnalyzers()
    {
        // Exclude MinIterationTimeAnalyser that cause warning when using RunStrategy.Monitoring.
        var analyzers = DefaultConfig.Instance
                                     .GetAnalysers()
                                     .Where(x => x is not MinIterationTimeAnalyser)
                                     .ToArray();

        AddAnalyser(analyzers);
    }
}
