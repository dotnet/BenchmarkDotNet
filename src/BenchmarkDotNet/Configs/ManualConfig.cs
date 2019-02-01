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

        [PublicAPI] public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;
        [PublicAPI] public bool KeepBenchmarkFiles { get; set; }
        [PublicAPI] public bool SummaryPerType { get; set; } = true;
        [PublicAPI] public string ArtifactsPath { get; set; }
        [PublicAPI] public Encoding Encoding { get; set; }
        [PublicAPI] public bool StopOnFirstError { get; set; }
        [PublicAPI] public IOrderer Orderer { get; set; }
        [PublicAPI] public SummaryStyle SummaryStyle { get; set; }

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
            Orderer = config.Orderer ?? Orderer;
            KeepBenchmarkFiles |= config.KeepBenchmarkFiles;
            SummaryPerType &= config.SummaryPerType;
            ArtifactsPath = config.ArtifactsPath ?? ArtifactsPath;
            Encoding = config.Encoding ?? Encoding;
            SummaryStyle = config.SummaryStyle ?? SummaryStyle;
            logicalGroupRules.AddRange(config.GetLogicalGroupRules());
            StopOnFirstError |= config.StopOnFirstError;
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
    }
}