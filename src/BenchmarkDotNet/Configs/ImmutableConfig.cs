using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
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
        private readonly ImmutableHashSet<BenchmarkLogicalGroupRule> rules;

        internal ImmutableConfig(
            ImmutableArray<IColumnProvider> uniqueColumnProviders,
            ImmutableHashSet<ILogger> uniqueLoggers,
            ImmutableHashSet<HardwareCounter> uniqueHardwareCounters,
            ImmutableHashSet<IDiagnoser> uniqueDiagnosers,
            ImmutableArray<IExporter> uniqueExporters,
            ImmutableHashSet<IAnalyser> unqueAnalyzers,
            ImmutableHashSet<IValidator> uniqueValidators,
            ImmutableHashSet<IFilter> uniqueFilters,
            ImmutableHashSet<BenchmarkLogicalGroupRule> uniqueRules,
            ImmutableHashSet<Job> uniqueRunnableJobs,
            ConfigUnionRule unionRule,
            string artifactsPath,
            Encoding encoding,
            IOrderer orderer,
            SummaryStyle summaryStyle,
            ConfigOptions options)
        {
            columnProviders = uniqueColumnProviders;
            loggers = uniqueLoggers;
            hardwareCounters = uniqueHardwareCounters;
            diagnosers = uniqueDiagnosers;
            exporters = uniqueExporters;
            analysers = unqueAnalyzers;
            validators = uniqueValidators;
            filters = uniqueFilters;
            rules = uniqueRules;
            jobs = uniqueRunnableJobs;
            UnionRule = unionRule;
            ArtifactsPath = artifactsPath;
            Encoding = encoding;
            Orderer = orderer;
            SummaryStyle = summaryStyle;
            Options = options;
        }

        public ConfigUnionRule UnionRule { get; }
        public string ArtifactsPath { get; }
        public Encoding Encoding { get; }
        public ConfigOptions Options { get; }
        [NotNull] public IOrderer Orderer { get; }
        public SummaryStyle SummaryStyle { get; }

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

        public ILogger GetCompositeLogger() => new CompositeLogger(loggers);
        public IExporter GetCompositeExporter() => new CompositeExporter(exporters);
        public IValidator GetCompositeValidator() => new CompositeValidator(validators);
        public IAnalyser GetCompositeAnalyser() => new CompositeAnalyser(analysers);
        public IDiagnoser GetCompositeDiagnoser() => new CompositeDiagnoser(diagnosers);

        public bool HasMemoryDiagnoser() => diagnosers.Contains(MemoryDiagnoser.Default);

        public bool HasThreadingDiagnoser() => diagnosers.Contains(ThreadingDiagnoser.Default);

        public bool HasExtraStatsDiagnoser() => HasMemoryDiagnoser() || HasThreadingDiagnoser();

        public IDiagnoser GetCompositeDiagnoser(BenchmarkCase benchmarkCase, RunMode runMode)
        {
            var diagnosersForGivenMode = diagnosers.Where(diagnoser => diagnoser.GetRunMode(benchmarkCase) == runMode).ToImmutableHashSet();

            return diagnosersForGivenMode.Any() ? new CompositeDiagnoser(diagnosersForGivenMode) : null;
        }
    }
}