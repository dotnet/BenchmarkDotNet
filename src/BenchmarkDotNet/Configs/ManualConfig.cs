﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
        private readonly HashSet<HardwareCounter> hardwareCounters = new HashSet<HardwareCounter>();
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
        [PublicAPI] public CultureInfo CultureInfo { get; set; }
        [PublicAPI] public IOrderer Orderer { get; set; }
        [PublicAPI] public SummaryStyle SummaryStyle { get; set; }

        public ManualConfig WithOption(ConfigOptions option, bool value)
        {
            Options = Options.Set(value, option);
            return this;
        }
        
        public ManualConfig WithOptions(ConfigOptions options)
        {
            Options |= options;
            return this;
        }

        public ManualConfig WithUnionRule(ConfigUnionRule unionRule)
        {
            UnionRule = unionRule;
            return this;
        }

        public ManualConfig WithArtifactsPath(string artifactsPath)
        {
            ArtifactsPath = artifactsPath;
            return this;
        }

        public ManualConfig WithSummaryStyle(SummaryStyle summaryStyle)
        {
            SummaryStyle = summaryStyle;
            return this;
        }

        public ManualConfig WithOrderer(IOrderer orderer)
        {
            Orderer = orderer;
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddColumn() instead.")]
        public void Add(params IColumn[] newColumns) => AddColumn(newColumns);

        public ManualConfig AddColumn(params IColumn[] newColumns)
        {
            columnProviders.AddRange(newColumns.Select(c => c.ToProvider()));
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddColumnProvider() instead.")]
        public void Add(params IColumnProvider[] newColumnProviders) => AddColumnProvider(newColumnProviders);

        public ManualConfig AddColumnProvider(params IColumnProvider[] newColumnProviders)
        {
            columnProviders.AddRange(newColumnProviders);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddExporter() instead.")]
        public void Add(params IExporter[] newExporters) => AddExporter(newExporters);

        public ManualConfig AddExporter(params IExporter[] newExporters)
        {
            exporters.AddRange(newExporters);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddLogger() instead.")]
        public void Add(params ILogger[] newLoggers) => AddLogger(newLoggers);

        public ManualConfig AddLogger(params ILogger[] newLoggers)
        {
            loggers.AddRange(newLoggers);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddDiagnoser() instead.")]
        public void Add(params IDiagnoser[] newDiagnosers) => AddDiagnoser(newDiagnosers);

        public ManualConfig AddDiagnoser(params IDiagnoser[] newDiagnosers)
        {
            diagnosers.AddRange(newDiagnosers);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddAnalyser() instead.")]
        public void Add(params IAnalyser[] newAnalysers) => AddAnalyser(newAnalysers);

        public ManualConfig AddAnalyser(params IAnalyser[] newAnalysers)
        {
            analysers.AddRange(newAnalysers);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddValidator() instead.")]
        public void Add(params IValidator[] newValidators) => AddValidator(newValidators);

        public ManualConfig AddValidator(params IValidator[] newValidators)
        {
            validators.AddRange(newValidators);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddJob() instead.")]
        public void Add(params Job[] newJobs) => AddJob(newJobs);

        public ManualConfig AddJob(params Job[] newJobs)
        {
            jobs.AddRange(newJobs.Select(j => j.Freeze())); // DONTTOUCH: please DO NOT remove .Freeze() call.
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using ..AddHardwareCounters()() instead.")]
        public void Add(params HardwareCounter[] newHardwareCounters) => AddHardwareCounters(newHardwareCounters);
        
        public ManualConfig AddHardwareCounters(params HardwareCounter[] newHardwareCounters)
        {
            hardwareCounters.AddRange(newHardwareCounters);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddFilter() instead.")]
        public void Add(params IFilter[] newFilters) => AddFilter(newFilters);
        
        public ManualConfig AddFilter(params IFilter[] newFilters)
        {
            filters.AddRange(newFilters);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method will soon be removed, please start using .AddLogicalGroupRules() instead.")]
        public void Add(params BenchmarkLogicalGroupRule[] rules) => AddLogicalGroupRules(rules);
        
        public ManualConfig AddLogicalGroupRules(params BenchmarkLogicalGroupRule[] rules)
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
            CultureInfo = config.CultureInfo ?? CultureInfo;
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