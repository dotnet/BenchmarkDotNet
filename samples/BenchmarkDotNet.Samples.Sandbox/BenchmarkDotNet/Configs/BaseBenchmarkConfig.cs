using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet;

public class BaseBenchmarkConfig : ManualConfig
{
    public BaseBenchmarkConfig()
    {
        WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(40)); // Default: 20 chars
        WithBuildTimeout(TimeSpan.FromMinutes(10)); // Default: 120 seconds

        WithOrderer(new DefaultOrderer());
        WithUnionRule(ConfigUnionRule.Union);
        WithArtifactsPath(DefaultConfig.Instance.ArtifactsPath!);

#if DEBUG
        // Allow benchmarks for debug build.
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
#endif

        // Enable following settings for debugging
        // WithOptions(ConfigOptions.StopOnFirstError);
        // WithOptions(ConfigOptions.KeepBenchmarkFiles);
        // WithOptions(ConfigOptions.GenerateMSBuildBinLog);
    }

    // Use ShortRun based settings (LaunchCount=1 IterationCount=3 WarmupCount = 3)
    // And use RecommendedConfig setting that used by `dotnet/performance` repository.
    // https://github.com/dotnet/performance/blob/main/src/harness/BenchmarkDotNet.Extensions/RecommendedConfig.cs
    protected virtual Job GetBaseJobConfig() =>
        Job.Default
           .WithLaunchCount(1)
           .WithWarmupCount(3)
           .WithIterationTime(TimeInterval.FromMilliseconds(250)) // Default: 500 [ms]
           .WithMinIterationCount(15)  // Default: 15
           .WithMaxIterationCount(20); // Default: 100

    /// <summary>
    /// Add configurations.
    /// </summary>
    protected void AddConfigurations()
    {
        AddAnalyzers();
        AddColumnHidingRules();
        AddColumnProviders();
        AddDiagnosers();
        AddEventProcessors();
        AddExporters();
        AddFilters();
        AddHardwareCounters();
        AddLoggers();
        AddLogicalGroupRules();
        AddValidators();
    }

    protected virtual void AddAnalyzers()
    {
        AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
    }

    protected virtual void AddColumnHidingRules()
    {
    }

    protected virtual void AddColumnProviders()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
    }

    protected virtual void AddDiagnosers()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new ExceptionDiagnoser(new ExceptionDiagnoserConfig(displayExceptionsIfZeroValue: false)));

#if NETCOREAPP3_0_OR_GREATER
        AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayCompletedWorkItemCountWhenZero: false, displayLockContentionWhenZero: false)));
#endif
    }

    protected virtual void AddExporters()
    {
        // Use ConsoleMarkdownExporter to disable group higligting with `**`.
        AddExporter(MarkdownExporter.Console);
    }

    protected virtual void AddEventProcessors()
    {
    }

    protected virtual void AddFilters()
    {
        AddFilter(TargetFrameworkFilter.Instance);
    }

    protected virtual void AddHardwareCounters()
    {
    }

    protected virtual void AddLoggers()
    {
        AddLogger(ConsoleLogger.Default);
    }

    protected virtual void AddLogicalGroupRules()
    {
    }

    protected virtual void AddValidators()
    {
        AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
    }
}
