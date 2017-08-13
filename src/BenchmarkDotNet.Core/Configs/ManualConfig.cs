using System;
using System.Collections.Generic;
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
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Reports;

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
        private IOrderProvider orderProvider = null;
        private ISummaryStyle summaryStyle = null;

        public IEnumerable<IColumnProvider> GetColumnProviders() => columnProviders;
        public IEnumerable<ILogger> GetLoggers() => loggers;
        
        public IEnumerable<IAnalyser> GetAnalysers() => analysers;
        public IEnumerable<IValidator> GetValidators() => validators;
        public IEnumerable<Job> GetJobs() => jobs;
        public IEnumerable<HardwareCounter> GetHardwareCounters() => hardwareCounters;
        public IEnumerable<IFilter> GetFilters() => filters;
        public IOrderProvider GetOrderProvider() => orderProvider;
        public ISummaryStyle GetSummaryStyle() => summaryStyle;

        public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;

        public bool KeepBenchmarkFiles { get; set; }

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
        public void Set(IOrderProvider provider) => orderProvider = provider ?? orderProvider;
        public void Set(ISummaryStyle style) => summaryStyle = style ?? summaryStyle;

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
            orderProvider = config.GetOrderProvider() ?? orderProvider;
            KeepBenchmarkFiles |= config.KeepBenchmarkFiles;
            summaryStyle = summaryStyle ?? config.GetSummaryStyle();
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
                    break;
                case ConfigUnionRule.AlwaysUseGlobal:
                    manualConfig.Add(globalConfig);
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

        public static IConfig Parse(string[] args) => new ConfigParser().Parse(args);

        public static void PrintOptions(ILogger logger, int prefixWidth, int outputWidth)
            => new ConfigParser().PrintOptions(logger, prefixWidth: prefixWidth, outputWidth: outputWidth);
    }
}