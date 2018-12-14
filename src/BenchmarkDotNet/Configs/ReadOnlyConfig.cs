using System;
using System.Collections.Generic;
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
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Configs
{
    [PublicAPI]
    public class ReadOnlyConfig : IConfig
    {
        private readonly IConfig config;

        public ReadOnlyConfig([NotNull] IConfig config) 
            => this.config = config ?? throw new ArgumentNullException(nameof(config));

        public IEnumerable<IColumnProvider> GetColumnProviders() => config.GetColumnProviders();
        public IEnumerable<IExporter> GetExporters() => config.GetExporters();
        public IEnumerable<ILogger> GetLoggers() => config.GetLoggers();
        public IEnumerable<IDiagnoser> GetDiagnosers() => config.GetDiagnosers();
        public IEnumerable<IAnalyser> GetAnalysers() => config.GetAnalysers();
        public IEnumerable<Job> GetJobs() => config.GetJobs();
        public IEnumerable<IValidator> GetValidators() => config.GetValidators();
        public IEnumerable<HardwareCounter> GetHardwareCounters() => config.GetHardwareCounters();
        public IEnumerable<IFilter> GetFilters() => config.GetFilters();

        public IOrderer GetOrderer() => config.GetOrderer();
        public ISummaryStyle GetSummaryStyle() => config.GetSummaryStyle();

        public ConfigUnionRule UnionRule => config.UnionRule;

        public bool KeepBenchmarkFiles => config.KeepBenchmarkFiles;
        public bool SummaryPerType => config.SummaryPerType;

        public string ArtifactsPath => config.ArtifactsPath;

        public Encoding Encoding => config.Encoding;

        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => config.GetLogicalGroupRules();

        public bool StopOnFirstError => config.StopOnFirstError;
    }
}