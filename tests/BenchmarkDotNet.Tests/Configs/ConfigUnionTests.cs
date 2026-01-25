using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.EventProcessors;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs;

public class ConfigUnionTests
{
    [Fact]
    public void UnionConfig()
    {
        // Arrange
        var config = CreateDummyConfig();
        var expected = config.CreateImmutableConfig();

        // Act
        config = ManualConfig.Union(config, CreateDummyConfig());
        var result = config.CreateImmutableConfig();

        // Assert
        Validate(expected, result);
    }

    [Fact]
    public void UnionConfigByAddMethods()
    {
        var p1 = new SimpleColumnProvider(TargetMethodColumn.Namespace).GetHashCode();
        var p2 = new SimpleColumnProvider(TargetMethodColumn.Namespace).GetHashCode();

        // Arrange
        var config = CreateDummyConfig();
        var otherConfig = CreateDummyConfig();
        var expected = config.CreateImmutableConfig();

        // Act
        config.AddAnalyser(otherConfig.GetAnalysers().ToArray())
              .AddColumnProvider(otherConfig.GetColumnProviders().ToArray())
              .AddDiagnoser(otherConfig.GetDiagnosers().ToArray())
              .AddExporter(otherConfig.GetExporters().ToArray())
              .AddEventProcessor(otherConfig.GetEventProcessors().ToArray())
              .AddFilter(otherConfig.GetFilters().ToArray())
              .AddHardwareCounters(otherConfig.GetHardwareCounters().ToArray())
              .AddLogger(otherConfig.GetLoggers().ToArray())
              .AddLogicalGroupRules(otherConfig.GetLogicalGroupRules().ToArray())
              .AddValidator(otherConfig.GetValidators().ToArray());

        var result = config.CreateImmutableConfig();

        // Assert
        Validate(expected, result);
    }

    private static void Validate(ImmutableConfig expected, ImmutableConfig result)
    {
        Assert.Equal(expected.GetAnalysers(), result.GetAnalysers());
        Assert.Equal(expected.GetColumnProviders(), result.GetColumnProviders());
        Assert.Equal(expected.GetDiagnosers(), result.GetDiagnosers());
        Assert.Equal(expected.GetExporters(), result.GetExporters());
        Assert.Equal(expected.GetEventProcessors(), result.GetEventProcessors());
        Assert.Equal(expected.GetFilters(), result.GetFilters());
        Assert.Equal(expected.GetHardwareCounters(), result.GetHardwareCounters());
        Assert.Equal(expected.GetLoggers(), result.GetLoggers());
        Assert.Equal(expected.GetLogicalGroupRules(), result.GetLogicalGroupRules());
        Assert.Equal(expected.GetValidators(), result.GetValidators());
    }

    private static ManualConfig CreateDummyConfig() =>
        ManualConfig.CreateEmpty()
                    .AddAnalyser([EnvironmentAnalyser.Default])
                    .AddColumn(TargetMethodColumn.Namespace)
                    .AddColumnProvider(DefaultColumnProviders.Instance)
                    .AddDiagnoser([MemoryDiagnoser.Default])
                    .AddExporter(MarkdownExporter.Default)
                    .AddEventProcessor(DummyEventProcessor.Instance)
                    .AddFilter(DummyFilter.Instance)
                    .AddHardwareCounters([HardwareCounter.BranchMispredictions])
                    .AddLogger(ConsoleLogger.Default)
                    .AddLogicalGroupRules([BenchmarkLogicalGroupRule.ByCategory])
                    .AddValidator([BaselineValidator.FailOnError])
                    .HideColumns([Column.Id]);

    private class DummyFilter : IFilter
    {
        public static DummyFilter Instance = new DummyFilter();

        private DummyFilter() { }

        public bool Predicate(BenchmarkCase benchmarkCase) => true;
    }

    private class DummyEventProcessor : EventProcessor
    {
        public static DummyEventProcessor Instance = new DummyEventProcessor();

        private DummyEventProcessor() { }
    }
}
