using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
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
using BenchmarkDotNet.Validators;
using RunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.Configs
{
    public sealed class ImmutableConfig : IConfig
    {
        // if something is an array here instead of hashset it means it must have a guaranteed order of elements
        private readonly ImmutableArray<IColumnProvider> columnProviders;
        private readonly ImmutableArray<IExporter> exporters;
        private readonly ImmutableHashSet<ILogger> loggers;
        private readonly ImmutableHashSet<IDiagnoser> diagnosers;
        private readonly ImmutableHashSet<IAnalyser> analysers;
        private readonly ImmutableHashSet<IValidator> validators;
        private readonly ImmutableHashSet<Job> jobs;
        private readonly ImmutableHashSet<HardwareCounter> hardwareCounters;
        private readonly ImmutableHashSet<IFilter> filters;
        private readonly ImmutableArray<BenchmarkLogicalGroupRule> rules;
        private readonly ImmutableHashSet<EventProcessor> eventProcessors;
        private readonly ImmutableArray<IColumnHidingRule> columnHidingRules;

        internal ImmutableConfig(
            ImmutableArray<IColumnProvider> uniqueColumnProviders,
            ImmutableHashSet<ILogger> uniqueLoggers,
            ImmutableHashSet<HardwareCounter> uniqueHardwareCounters,
            ImmutableHashSet<IDiagnoser> uniqueDiagnosers,
            ImmutableArray<IExporter> uniqueExporters,
            ImmutableHashSet<IAnalyser> uniqueAnalyzers,
            ImmutableHashSet<IValidator> uniqueValidators,
            ImmutableHashSet<IFilter> uniqueFilters,
            ImmutableArray<BenchmarkLogicalGroupRule> uniqueRules,
            ImmutableArray<IColumnHidingRule> uniqueColumnHidingRules,
            ImmutableHashSet<Job> uniqueRunnableJobs,
            ImmutableHashSet<EventProcessor> uniqueEventProcessors,
            ConfigUnionRule unionRule,
            string artifactsPath,
            CultureInfo cultureInfo,
            IOrderer orderer,
            ICategoryDiscoverer categoryDiscoverer,
            SummaryStyle summaryStyle,
            ConfigOptions options,
            TimeSpan buildTimeout,
            IReadOnlyList<Conclusion> configAnalysisConclusion)
        {
            columnProviders = uniqueColumnProviders;
            loggers = uniqueLoggers;
            hardwareCounters = uniqueHardwareCounters;
            diagnosers = uniqueDiagnosers;
            exporters = uniqueExporters;
            analysers = uniqueAnalyzers;
            validators = uniqueValidators;
            filters = uniqueFilters;
            rules = uniqueRules;
            columnHidingRules = uniqueColumnHidingRules;
            jobs = uniqueRunnableJobs;
            eventProcessors = uniqueEventProcessors;
            UnionRule = unionRule;
            ArtifactsPath = artifactsPath;
            CultureInfo = cultureInfo;
            Orderer = orderer;
            CategoryDiscoverer = categoryDiscoverer;
            SummaryStyle = summaryStyle;
            Options = options;
            BuildTimeout = buildTimeout;
            ConfigAnalysisConclusion = configAnalysisConclusion;
        }

        public ConfigUnionRule UnionRule { get; }
        public string ArtifactsPath { get; }
        public CultureInfo CultureInfo { get; }
        public ConfigOptions Options { get; }
        public IOrderer Orderer { get; }
        public ICategoryDiscoverer CategoryDiscoverer { get; }
        public SummaryStyle SummaryStyle { get; }
        public TimeSpan BuildTimeout { get; }

        public IEnumerable<IColumnProvider> GetColumnProviders() => columnProviders;
        public IEnumerable<IExporter> GetExporters() => exporters;
        public IEnumerable<ILogger> GetLoggers() => loggers;
        public IEnumerable<IDiagnoser> GetDiagnosers() => diagnosers;
        public IEnumerable<IAnalyser> GetAnalysers() => analysers;
        public IEnumerable<Job> GetJobs() => jobs;
        public IEnumerable<IValidator> GetValidators() => validators;
        public IEnumerable<HardwareCounter> GetHardwareCounters() => hardwareCounters;
        public IEnumerable<IFilter> GetFilters() => filters;
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => rules;
        public IEnumerable<EventProcessor> GetEventProcessors() => eventProcessors;
        public IEnumerable<IColumnHidingRule> GetColumnHidingRules() => columnHidingRules;

        public ILogger GetCompositeLogger() => new CompositeLogger(loggers);
        public IExporter GetCompositeExporter() => new CompositeExporter(exporters);
        public IValidator GetCompositeValidator() => new CompositeValidator(validators);
        public IAnalyser GetCompositeAnalyser() => new CompositeAnalyser(analysers);
        public IDiagnoser GetCompositeDiagnoser() => new CompositeDiagnoser(diagnosers);

        public bool HasMemoryDiagnoser() => diagnosers.OfType<MemoryDiagnoser>().Any();

        public bool HasThreadingDiagnoser() => diagnosers.Contains(ThreadingDiagnoser.Default);

        public bool HasExceptionDiagnoser() => diagnosers.Contains(ExceptionDiagnoser.Default);

        internal bool HasPerfCollectProfiler() => diagnosers.OfType<PerfCollectProfiler>().Any();

        public bool HasExtraStatsDiagnoser() => HasMemoryDiagnoser() || HasThreadingDiagnoser() || HasExceptionDiagnoser();

        public IDiagnoser? GetCompositeDiagnoser(BenchmarkCase benchmarkCase, RunMode runMode)
        {
            var diagnosersForGivenMode = diagnosers.Where(diagnoser => diagnoser.GetRunMode(benchmarkCase) == runMode).ToImmutableHashSet();

            return diagnosersForGivenMode.Any() ? new CompositeDiagnoser(diagnosersForGivenMode) : null;
        }

        public IReadOnlyList<Conclusion> ConfigAnalysisConclusion { get; private set; }
    }
}
