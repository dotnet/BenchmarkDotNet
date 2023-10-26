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

namespace BenchmarkDotNet.Configs
{
    /// <summary>
    /// config which allows to debug benchmarks running it in the same process
    /// </summary>
    [PublicAPI]
    public class DebugInProcessConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs()
            => new[]
            {
                Job.Default
                    .WithToolchain(
                        new InProcessEmitToolchain(
                            TimeSpan.FromHours(1), // 1h should be enough to debug the benchmark
                            true))
            };
    }

    /// <summary>
    /// config which allows to build benchmarks in Debug
    /// </summary>
    [PublicAPI]
    public class DebugBuildConfig : DebugConfig
    {
        public override IEnumerable<Job> GetJobs()
            => new[]
            {
                Job.Default
                    .WithCustomBuildConfiguration("Debug") // will do `-c Debug everywhere`
            };
    }

    public abstract class DebugConfig : IConfig
    {
        private readonly static Conclusion[] emptyConclusion = Array.Empty<Conclusion>();
        public abstract IEnumerable<Job> GetJobs();

        public IEnumerable<IValidator> GetValidators() => Array.Empty<IValidator>();
        public IEnumerable<IColumnProvider> GetColumnProviders() => DefaultColumnProviders.Instance;
        public IEnumerable<IExporter> GetExporters() => Array.Empty<IExporter>();
        public IEnumerable<ILogger> GetLoggers() => new[] { ConsoleLogger.Default };
        public IEnumerable<IDiagnoser> GetDiagnosers() => Array.Empty<IDiagnoser>();
        public IEnumerable<IAnalyser> GetAnalysers() => Array.Empty<IAnalyser>();
        public IEnumerable<HardwareCounter> GetHardwareCounters() => Array.Empty<HardwareCounter>();
        public IEnumerable<EventProcessor> GetEventProcessors() => Array.Empty<EventProcessor>();
        public IEnumerable<IFilter> GetFilters() => Array.Empty<IFilter>();
        public IEnumerable<IColumnHidingRule> GetColumnHidingRules() => Array.Empty<IColumnHidingRule>();

        public IOrderer Orderer => DefaultOrderer.Instance;
        public ICategoryDiscoverer? CategoryDiscoverer => DefaultCategoryDiscoverer.Instance;
        public SummaryStyle SummaryStyle => SummaryStyle.Default;
        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;
        public TimeSpan BuildTimeout => DefaultConfig.Instance.BuildTimeout;

        public string ArtifactsPath => null; // DefaultConfig.ArtifactsPath will be used if the user does not specify it in explicit way

        public CultureInfo CultureInfo => null;
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => Array.Empty<BenchmarkLogicalGroupRule>();

        public ConfigOptions Options => ConfigOptions.KeepBenchmarkFiles | ConfigOptions.DisableOptimizationsValidator;

        public IReadOnlyList<Conclusion> ConfigAnalysisConclusion => emptyConclusion;
    }
}