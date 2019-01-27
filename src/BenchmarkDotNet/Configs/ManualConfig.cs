using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Configs
{
    public class ManualConfig : IConfig
    {
        private readonly List<IColumnProvider> columnProviders = new List<IColumnProvider>();
        private readonly List<IExporter> exporters = new List<IExporter>();
        private readonly List<ILogger> loggers = new List<ILogger>();
        private readonly List<IDiagnoser> diagnosers = new List<IDiagnoser>();
        private readonly List<IAnalyser> analysers = new List<IAnalyser>();
        private readonly List<IValidator> validators = new List<IValidator>();
        private readonly List<Job> jobs = new List<Job>();
        private readonly List<HardwareCounter> hardwareCounters = new List<HardwareCounter>();
        private readonly List<IFilter> filters = new List<IFilter>();
        private IOrderer orderer;
        private ISummaryStyle summaryStyle;
        private readonly HashSet<BenchmarkLogicalGroupRule> logicalGroupRules = new HashSet<BenchmarkLogicalGroupRule>();

        public IEnumerable<IColumnProvider> GetColumnProviders() => columnProviders;
        public IEnumerable<ILogger> GetLoggers() => loggers;
        
        public IEnumerable<IValidator> GetValidators() => validators;
        public IEnumerable<Job> GetJobs() => jobs;
        public IEnumerable<HardwareCounter> GetHardwareCounters() => hardwareCounters;
        public IEnumerable<IFilter> GetFilters() => filters;
        public IOrderer GetOrderer() => orderer;
        public ISummaryStyle GetSummaryStyle() => summaryStyle;
        
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => logicalGroupRules;
        [PublicAPI] public bool StopOnFirstError { get; set; }
        [PublicAPI] public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;
        [PublicAPI] public bool KeepBenchmarkFiles { get; set; }
        [PublicAPI] public bool SummaryPerType { get; set; } = true;
        [PublicAPI] public string ArtifactsPath { get; set; }
        [PublicAPI] public Encoding Encoding { get; set; }

        public void Add(params IColumn[] newColumns) => columnProviders.AddRange(newColumns.Select(c => c.ToProvider()));
        public void Add(params IColumnProvider[] newColumnProviders) => columnProviders.AddRange(newColumnProviders);
        public void Add(params IExporter[] newExporters) => exporters.AddRange(newExporters);
        public void Add(params ILogger[] newLoggers) => loggers.AddRange(newLoggers);
        public void Add(params IDiagnoser[] newDiagnosers) => diagnosers.AddRange(newDiagnosers);
        public void Add(params IAnalyser[] newAnalysers) => analysers.AddRange(newAnalysers);
        public void Add(params IValidator[] newValidators) => validators.AddRange(newValidators);
        public void Add(params Job[] newJobs) => jobs.AddRange(newJobs.Select(j => j.Freeze())); // DONTTOUCH: please DO NOT remove .Freeze() call.
        public void Add(params HardwareCounter[] newHardwareCounters) => hardwareCounters.AddRange(newHardwareCounters);
        public void Add(params IFilter[] newFilters) => filters.AddRange(newFilters);
        public void Set(IOrderer provider) => orderer = provider ?? orderer;
        public void Set(ISummaryStyle style) => summaryStyle = style ?? summaryStyle;
        public void Set(Encoding encoding) => Encoding = encoding;
        public void Add(params BenchmarkLogicalGroupRule[] rules) => logicalGroupRules.AddRange(rules);

        [PublicAPI]
        public void Add(IConfig config)
        {
            columnProviders.AddRange(config.GetColumnProviders());
            exporters.AddRange(config.GetExporters());
            loggers.AddRange(config.GetLoggers());
            diagnosers.AddRange(config.GetDiagnosers());
            analysers.AddRange(config.GetAnalysers());
            jobs.AddRange(config.GetJobs());
            validators.AddRange(config.GetValidators());
            hardwareCounters.AddRange(config.GetHardwareCounters());
            filters.AddRange(config.GetFilters());
            orderer = config.GetOrderer() ?? orderer;
            KeepBenchmarkFiles |= config.KeepBenchmarkFiles;
            SummaryPerType &= config.SummaryPerType;
            ArtifactsPath = config.ArtifactsPath ?? ArtifactsPath;
            Encoding = config.Encoding ?? Encoding;
            summaryStyle = summaryStyle ?? config.GetSummaryStyle();
            logicalGroupRules.AddRange(config.GetLogicalGroupRules());
            StopOnFirstError |= config.StopOnFirstError;
        }

        public IEnumerable<IDiagnoser> GetDiagnosers()
        {
            if (hardwareCounters.IsEmpty() || diagnosers.OfType<IHardwareCountersDiagnoser>().Any())
                return diagnosers;

            var hardwareCountersDiagnoser = DiagnosersLoader.GetImplementation<IHardwareCountersDiagnoser>();

            if(hardwareCountersDiagnoser != default(IDiagnoser))
                return diagnosers.Union(new [] { hardwareCountersDiagnoser });

            return diagnosers;
        }

        public IEnumerable<IExporter> GetExporters()
        {
            var allDiagnosers = GetDiagnosers().ToArray();

            foreach (var exporter in exporters)
                yield return exporter;

            foreach (var diagnoser in allDiagnosers)
                foreach (var exporter in diagnoser.Exporters)
                    yield return exporter;

            var hardwareCounterDiagnoser = allDiagnosers.OfType<IHardwareCountersDiagnoser>().SingleOrDefault();
            var disassemblyDiagnoser = allDiagnosers.OfType<IDisassemblyDiagnoser>().SingleOrDefault();

            if (hardwareCounterDiagnoser != null && disassemblyDiagnoser != null)
                yield return new InstructionPointerExporter(hardwareCounterDiagnoser, disassemblyDiagnoser);
        }


        public IEnumerable<IAnalyser> GetAnalysers()
        {
            var allDiagnosers = GetDiagnosers().ToArray();

            foreach (var analyser in analysers)
                yield return analyser;

            foreach (var diagnoser in allDiagnosers)
            foreach (var analyser in diagnoser.Analysers)
                yield return analyser;
        }

        public static ManualConfig CreateEmpty() => new ManualConfig();

        public static ManualConfig Create(IConfig config)
        {
            var manualConfig = new ManualConfig();
            manualConfig.Add(config);
            return manualConfig;
        }

        public static ManualConfig Union(IConfig globalConfig, IConfig localConfig)
        {
            var manualConfig = new ManualConfig();
            switch (localConfig.UnionRule)
            {
                case ConfigUnionRule.AlwaysUseLocal:
                    manualConfig.Add(localConfig);
                    manualConfig.Add(globalConfig.GetFilters().ToArray()); // the filters should be merged anyway
                    break;
                case ConfigUnionRule.AlwaysUseGlobal:
                    manualConfig.Add(globalConfig);
                    manualConfig.Add(localConfig.GetFilters().ToArray()); // the filters should be merged anyway
                    break;
                case ConfigUnionRule.Union:
                    manualConfig.Add(globalConfig);
                    manualConfig.Add(localConfig);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return manualConfig;
        }

        public static ManualConfig UnionWithMandatory(IConfig sourceConfig)
        {
            var manualConfig = new ManualConfig();
            manualConfig.Add(sourceConfig);
            manualConfig.Add(GetMandatoryValidators());
            return manualConfig;
        }

        public static ReadOnlyConfig Cleanup(IConfig config)
        {
            // Same as .Add() but includes Cleanup calls if required.

            var cleaned = new ManualConfig();
            cleaned.columnProviders.AddRange(config.GetColumnProviders());
            cleaned.exporters.AddRange(config.GetExporters());
            cleaned.loggers.AddRange(config.GetLoggers());
            cleaned.diagnosers.AddRange(config.GetDiagnosers());
            cleaned.analysers.AddRange(config.GetAnalysers());
            cleaned.jobs.AddRange(config.GetJobs());
            cleaned.validators.AddRange(CleanupValidators(config.GetValidators()));
            cleaned.hardwareCounters.AddRange(config.GetHardwareCounters());
            cleaned.filters.AddRange(config.GetFilters());
            cleaned.orderer = config.GetOrderer() ?? cleaned.orderer;
            cleaned.KeepBenchmarkFiles |= config.KeepBenchmarkFiles;
            cleaned.SummaryPerType &= config.SummaryPerType;
            cleaned.ArtifactsPath = config.ArtifactsPath ?? cleaned.ArtifactsPath;
            cleaned.Encoding = config.Encoding ?? cleaned.Encoding;
            cleaned.summaryStyle = cleaned.summaryStyle ?? config.GetSummaryStyle();
            cleaned.logicalGroupRules.AddRange(config.GetLogicalGroupRules());
            cleaned.StopOnFirstError |= config.StopOnFirstError;

            return cleaned.AsReadOnly();
        }

        // Taken from CompositeValidator
        private static IValidator[] CleanupValidators(IEnumerable<IValidator> validators) =>
            validators
                .GroupBy(validator => validator.GetType())
                .Select(groupedByType => groupedByType.FirstOrDefault(validator => validator.TreatsWarningsAsErrors) ?? groupedByType.First())
                .Distinct()
                .ToArray();

        // TODO: place to somewhere else. Create a MandatoryConfig, maybe?
        private static readonly IValidator[] MandatoryValidators =
        {
            BaselineValidator.FailOnError,
            SetupCleanupValidator.FailOnError,
            RunModeValidator.FailOnError,
            DiagnosersValidator.Default,
            CompilationValidator.Default,
            ConfigValidator.Default,
            ShadowCopyValidator.Default,
            JitOptimizationsValidator.DontFailOnError,
            DeferredExecutionValidator.DontFailOnError
        };

        public static IValidator[] GetMandatoryValidators() => MandatoryValidators.ToArray();
    }
}