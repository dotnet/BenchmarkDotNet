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

        public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;

        public bool KeepBenchmarkFiles { get; set; }

        public bool SummaryPerType { get; set; } = true;

        public string ArtifactsPath { get; set; }

        public Encoding Encoding { get; set; }
        
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

        public void Add(IConfig config)
        {
            columnProviders.AddRange(config.GetColumnProviders());
            exporters.AddRange(config.GetExporters());
            loggers.AddRange(config.GetLoggers());
            diagnosers.AddRange(config.GetDiagnosers());
            analysers.AddRange(config.GetAnalysers());
            AddJobs(config.GetJobs());
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
        }

        public IEnumerable<IDiagnoser> GetDiagnosers()
        {
            if (hardwareCounters.IsEmpty())
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

        private void AddJobs(IEnumerable<Job> toAdd)
        {
            foreach (var notMutator in toAdd.Where(job => !job.Meta.IsMutator))
                jobs.Add(notMutator);
            
            var mutators = toAdd.Where(job => job.Meta.IsMutator).ToArray();
            if (!mutators.Any())
                return;
            
            if (!jobs.Any())
                jobs.Add(Job.Default);

            for (int i = 0; i < jobs.Count; i++)
            {
                var copy = jobs[i].UnfreezeCopy();

                foreach (var mutator in mutators)
                    copy.Apply(mutator);

                jobs[i] = copy.Freeze();
            }
        }
    }
}