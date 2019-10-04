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
        private readonly HashSet<BenchmarkLogicalGroupRule> logicalGroupRules = new HashSet<BenchmarkLogicalGroupRule>();

        public IEnumerable<IColumnProvider> GetColumnProviders() => columnProviders;
        public IEnumerable<IExporter> GetExporters() => exporters;
        public IEnumerable<ILogger> GetLoggers() => loggers;
        public IEnumerable<IDiagnoser> GetDiagnosers() => diagnosers;
        public IEnumerable<IAnalyser> GetAnalysers() => analysers;
        public IEnumerable<IValidator> GetValidators() => validators;
        public IEnumerable<Job> GetJobs() => jobs;
        public IEnumerable<HardwareCounter> GetHardwareCounters() => hardwareCounters;
        public IEnumerable<IFilter> GetFilters() => filters;
        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => logicalGroupRules;

        [PublicAPI] public ConfigOptions Options { get; set; }
        [PublicAPI] public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;
        [PublicAPI] public string ArtifactsPath { get; set; }
        [PublicAPI] public Encoding Encoding { get; set; }
        [PublicAPI] public IOrderer Orderer { get; set; }
        [PublicAPI] public SummaryStyle SummaryStyle { get; set; }

        [Obsolete("This property will soon be removed, please start using .Options instead")]
        public bool KeepBenchmarkFiles
        {
            get => Options.IsSet(ConfigOptions.KeepBenchmarkFiles);
            set => Options = Options.Set(value, ConfigOptions.KeepBenchmarkFiles);
        }

        [Obsolete("This property will soon be removed, please start using .Options instead")]
        public bool SummaryPerType
        {
            get => !Options.IsSet(ConfigOptions.JoinSummary);
            set => Options = Options.Set(!value, ConfigOptions.JoinSummary);
        }

        [Obsolete("This property will soon be removed, please start using .Options instead")]
        public bool StopOnFirstError
        {
            get => Options.IsSet(ConfigOptions.StopOnFirstError);
            set => Options = Options.Set(value, ConfigOptions.StopOnFirstError);
        }

        [Obsolete("This property will soon be removed, please start using AddColumn instead.")]
        public void Add(params IColumn[] newColumns) => AddColumn(newColumns);

        public IConfig AddColumn(params IColumn[] newColumns)
        {
            columnProviders.AddRange(newColumns.Select(c => c.ToProvider()));
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddColumnProvider instead.")]
        public void Add(params IColumnProvider[] newColumnProviders) => AddColumnProvider(newColumnProviders);

        public IConfig AddColumnProvider(params IColumnProvider[] newColumnProviders)
        {
            columnProviders.AddRange(newColumnProviders);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddExporter instead.")]
        public void Add(params IExporter[] newExporters) => AddExporter(newExporters);

        public IConfig AddExporter(params IExporter[] newExporters)
        {
            exporters.AddRange(newExporters);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddLogger instead.")]
        public void Add(params ILogger[] newLoggers) => AddLogger(newLoggers);

        public IConfig AddLogger(params ILogger[] newLoggers)
        {
            loggers.AddRange(newLoggers);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddDiagnoser instead.")]
        public void Add(params IDiagnoser[] newDiagnosers) => AddDiagnoser(newDiagnosers);

        public IConfig AddDiagnoser(params IDiagnoser[] newDiagnosers)
        {
            diagnosers.AddRange(newDiagnosers);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddAnalyser instead.")]
        public void Add(params IAnalyser[] newAnalysers) => AddAnalyser(newAnalysers);

        public IConfig AddAnalyser(params IAnalyser[] newAnalysers)
        {
            analysers.AddRange(newAnalysers);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddValidator instead.")]
        public void Add(params IValidator[] newValidators) => AddValidator(newValidators);

        public IConfig AddValidator(params IValidator[] newValidators)
        {
            validators.AddRange(newValidators);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddJob instead.")]
        public void Add(params Job[] newJobs) => AddJob(newJobs);

        public IConfig AddJob(params Job[] newJobs)
        {
            jobs.AddRange(newJobs.Select(j => j.Freeze())); // DONTTOUCH: please DO NOT remove .Freeze() call.
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddHardwareCounter instead.")]
        public void Add(params HardwareCounter[] newHardwareCounters) => AddHardwareCounter(newHardwareCounters);
        public IConfig AddHardwareCounter(params HardwareCounter[] newHardwareCounters)
        {
            hardwareCounters.AddRange(newHardwareCounters);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using AddFilter instead.")]
        public void Add(params IFilter[] newFilters) => AddFilter(newFilters);
        public IConfig AddFilter(params IFilter[] newFilters)
        {
            filters.AddRange(newFilters);
            return this;
        }

        [Obsolete("This property will soon be removed, please start using .GroupBenchmarksBy instead.")]
        public void Add(params BenchmarkLogicalGroupRule[] rules) => GroupBenchmarksBy(rules);
        public IConfig GroupBenchmarksBy(params BenchmarkLogicalGroupRule[] rules)
        {
            logicalGroupRules.AddRange(rules);
            return this;
        }


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
            Orderer = config.Orderer ?? Orderer;
            ArtifactsPath = config.ArtifactsPath ?? ArtifactsPath;
            Encoding = config.Encoding ?? Encoding;
            SummaryStyle = config.SummaryStyle ?? SummaryStyle;
            logicalGroupRules.AddRange(config.GetLogicalGroupRules());
            Options |= config.Options;
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
                    manualConfig.AddFilter(globalConfig.GetFilters().ToArray()); // the filters should be merged anyway
                    break;
                case ConfigUnionRule.AlwaysUseGlobal:
                    manualConfig.Add(globalConfig);
                    manualConfig.AddFilter(localConfig.GetFilters().ToArray()); // the filters should be merged anyway
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
    }
}