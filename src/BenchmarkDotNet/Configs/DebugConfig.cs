using System;
using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.EventProcessors;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;

using JetBrains.Annotations;

#nullable enable

namespace BenchmarkDotNet.Configs
{
    /// <summary>
    /// config which allows to debug benchmarks running it in the same process
    /// </summary>
    [PublicAPI]
    public class DebugInProcessConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs() =>
        [
            Job.Default
                .WithToolchain(InProcessEmitToolchain.Default)
        ];
    }

    /// <summary>
    /// config which allows to build benchmarks in Debug
    /// </summary>
    [PublicAPI]
    public class DebugBuildConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs() =>
        [
            Job.Default
                .WithCustomBuildConfiguration("Debug") // will do `-c Debug everywhere`
        ];
    }

    public abstract class DebugConfig : IConfig
    {
        private readonly static Conclusion[] emptyConclusion = [];
        public abstract IEnumerable<Job> GetJobs();

        public IEnumerable<IValidator> GetValidators() => [];
        public IEnumerable<IColumnProvider> GetColumnProviders() => DefaultColumnProviders.Instance;
        public IEnumerable<IExporter> GetExporters() => [];
        public IEnumerable<ILogger> GetLoggers() => [ConsoleLogger.Default];
        public IEnumerable<IDiagnoser> GetDiagnosers() => [];
        public IEnumerable<IAnalyser> GetAnalysers() => [];
        public IEnumerable<HardwareCounter> GetHardwareCounters() => [];
        public IEnumerable<EventProcessor> GetEventProcessors() => [];
        public IEnumerable<IFilter> GetFilters() => [];
        public IEnumerable<IColumnHidingRule> GetColumnHidingRules() => [];

        public IOrderer Orderer => DefaultOrderer.Instance;
        public ICategoryDiscoverer? CategoryDiscoverer => DefaultCategoryDiscoverer.Instance;
        public SummaryStyle SummaryStyle => SummaryStyle.Default;
        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;
        public TimeSpan BuildTimeout => DefaultConfig.Instance.BuildTimeout;
        public WakeLockType WakeLock => WakeLockType.None;

        public string? ArtifactsPath => null; // DefaultConfig.ArtifactsPath will be used if the user does not specify it in explicit way

        public CultureInfo? CultureInfo => null;
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => [];

        public ConfigOptions Options => ConfigOptions.KeepBenchmarkFiles | ConfigOptions.DisableOptimizationsValidator;

        public IReadOnlyList<Conclusion> ConfigAnalysisConclusion => emptyConclusion;
    }
}