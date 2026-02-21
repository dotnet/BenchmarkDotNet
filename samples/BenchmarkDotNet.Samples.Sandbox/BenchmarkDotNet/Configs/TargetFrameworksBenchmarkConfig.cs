using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet;

/// <summary>
/// BenchmarkConfig that run benchmarks for multiple target frameworks.
/// </summary>
public class TargetFrameworksBenchmarkConfig : BaseBenchmarkConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBenchmarkConfig"/> class.
    /// </summary>
    public TargetFrameworksBenchmarkConfig() : base()
    {
        // Configure base job config
        var baseJobConfig = GetBaseJobConfig();

        // Add jobs
        AddJob(baseJobConfig.WithToolchain(CsProjCoreToolchain.NetCoreApp80).WithId(".NET 8").AsBaseline());
        AddJob(baseJobConfig.WithToolchain(CsProjCoreToolchain.NetCoreApp90).WithId(".NET 9"));
        AddJob(baseJobConfig.WithToolchain(CsProjCoreToolchain.NetCoreApp10_0).WithId(".NET 10"));

        // AddJob(baseJobConfig.WithToolchain(CsProjClassicNetToolchain.Net48).WithId(".NET Framework 4.8"));

        // Configure additional settings.
        AddConfigurations();
    }

    protected override void AddLogicalGroupRules()
    {
        // Grouping benchmarks by method.
        // Note: When following conditions are met. BaselineCustomAnalyzer raise warning. See: https://github.com/dotnet/BenchmarkDotNet/issues/2956
        // 1. Job contains Baseline=true
        // 2. Benchmark method contains Baseline=true
        // 3. Enable grouping with method.
        AddLogicalGroupRules(
        [
            BenchmarkLogicalGroupRule.ByMethod,
        ]);
    }

    protected override void AddColumnHidingRules()
    {
        HideColumns(Column.Toolchain); // Toolchain information are shown at JobId column.
    }
}
