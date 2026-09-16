using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns;

public class SettingsColumnTests
{
    public class Bench
    {
        [Benchmark] public void Foo() { }
    }

    [Fact]
    public void SameToolchainWithDifferentSettingsShowsOnlyTheDifferingSetting()
    {
        // The two jobs use the same toolchain but different settings; job deduplication must keep them apart.
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.From(NativeAotRuntime.Net80,
                new NativeAotSettings { OptimizationPreference = "Speed" })))
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.From(NativeAotRuntime.Net80,
                new NativeAotSettings { OptimizationPreference = "Size" })));

        var columns = MockFactory.CreateSummary(typeof(Bench), config).GetColumns();

        // The setting the two jobs disagree on is shown...
        Assert.Contains(columns, c => c.ColumnName == nameof(NativeAotSettings.OptimizationPreference));
        // ...but a setting both jobs leave at the default is not.
        Assert.DoesNotContain(columns, c => c.ColumnName == nameof(NativeAotSettings.InstructionSet));
    }

    [Fact]
    public void DifferentToolchainsWithDefaultSettingsShowNoSettingColumns()
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.Net80))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80));

        var columns = MockFactory.CreateSummary(typeof(Bench), config).GetColumns();

        // A setting that only exists on one toolchain must not surface just because the other toolchain lacks it.
        Assert.DoesNotContain(columns, c => c.Id.StartsWith("Settings."));
    }

    [Fact]
    public void FileInfoSettingIsComparedByPathValueNotReference()
    {
        // Same CLI path passed as two distinct FileInfo instances; the jobs are kept apart by their differing
        // runtime. The path column must stay hidden because the values compare equal by FullName, not by reference.
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, new NetCoreAppSettings { CliPath = new FileInfo("dotnet") })))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core90, new NetCoreAppSettings { CliPath = new FileInfo("dotnet") })));

        var columns = MockFactory.CreateSummary(typeof(Bench), config).GetColumns();

        Assert.DoesNotContain(columns, c => c.ColumnName == nameof(NetCoreAppSettings.CliPath));
    }

    [Fact]
    public void FileInfoSettingWithDifferentPathsIsShown()
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, new NetCoreAppSettings { CliPath = new FileInfo("dotnet-a") })))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, new NetCoreAppSettings { CliPath = new FileInfo("dotnet-b") })));

        var columns = MockFactory.CreateSummary(typeof(Bench), config).GetColumns();

        Assert.Contains(columns, c => c.ColumnName == nameof(NetCoreAppSettings.CliPath));
    }

    [Fact]
    public void SharedKeyAcrossSettingsTypesRendersInASingleColumn()
    {
        // CliPath comes from the shared DotNetCliSettings base, so both NativeAOT and Core expose it. Differing
        // values across the two toolchains must surface as one column, not one per settings type.
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.From(NativeAotRuntime.Net80, new NativeAotSettings { CliPath = new FileInfo("dotnet-a") })))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, new NetCoreAppSettings { CliPath = new FileInfo("dotnet-b") })));

        var columns = MockFactory.CreateSummary(typeof(Bench), config).GetColumns();

        Assert.Single(columns, c => c.ColumnName == nameof(NetCoreAppSettings.CliPath));
    }

    [Fact]
    public void SettingMissingOnAToolchainRendersAsNotApplicable()
    {
        // RuntimeIdentifier is NativeAOT-only. The two NativeAOT jobs differ on it (so the column shows); the Core
        // job has no such setting and must render as "NA", not "?".
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.From(NativeAotRuntime.Net80, new NativeAotSettings { RuntimeIdentifier = "win-x64" })))
            .AddJob(Job.Dry.WithToolchain(CsProjNativeAotToolchain.From(NativeAotRuntime.Net80, new NativeAotSettings { RuntimeIdentifier = "linux-x64" })))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.NetCoreApp80));

        var summary = MockFactory.CreateSummary(typeof(Bench), config);
        var column = summary.GetColumns().Single(c => c.ColumnName == nameof(NativeAotSettings.RuntimeIdentifier));
        var coreCase = summary.BenchmarksCases.Single(bc => (bc.GetToolchain() as IHasSettings)?.Settings is NetCoreAppSettings);

        Assert.Equal("NA", column.GetValue(summary, coreCase));
    }

    [Fact]
    public void NullSettingValueRendersAsQuestionMark()
    {
        // Two Core jobs: one sets CliPath, the other leaves it null. The column shows (they differ), and the unset
        // one renders as "?" (the setting exists but is null), distinct from "NA".
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, new NetCoreAppSettings { CliPath = new FileInfo("dotnet") })))
            .AddJob(Job.Dry.WithToolchain(CsProjCoreToolchain.From(CoreRuntime.Core80, NetCoreAppSettings.Default)));

        var summary = MockFactory.CreateSummary(typeof(Bench), config);
        var column = summary.GetColumns().Single(c => c.ColumnName == nameof(NetCoreAppSettings.CliPath));
        var nullCase = summary.BenchmarksCases.Single(bc => (bc.GetToolchain() as IHasSettings)?.Settings is NetCoreAppSettings { CliPath: null });

        Assert.Equal("?", column.GetValue(summary, nullCase));
    }
}
