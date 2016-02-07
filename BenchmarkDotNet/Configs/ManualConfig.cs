using System.Collections.Generic;
using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Configs
{
    public class ManualConfig : IConfig
    {
        private readonly List<IColumn> columns = new List<IColumn>();
        private readonly List<IExporter> exporters = new List<IExporter>();
        private readonly List<ILogger> loggers = new List<ILogger>();
        private readonly List<IDiagnoser> diagnosers = new List<IDiagnoser>();
        private readonly List<IAnalyser> analysers = new List<IAnalyser>();
        private readonly List<IJob> jobs = new List<IJob>();

        public IEnumerable<IColumn> GetColumns() => columns;
        public IEnumerable<IExporter> GetExporters() => exporters;
        public IEnumerable<ILogger> GetLoggers() => loggers;
        public IEnumerable<IDiagnoser> GetDiagnosers() => diagnosers;
        public IEnumerable<IAnalyser> GetAnalysers() => analysers;
        public IEnumerable<IJob> GetJobs() => jobs;
        public ConfigUnionRule UnionRule { get; set; } = ConfigUnionRule.Union;

        public void Add(params IColumn[] newColumns) => columns.AddRange(newColumns);
        public void Add(params IExporter[] newExprters) => exporters.AddRange(newExprters);
        public void Add(params ILogger[] newLoggers) => loggers.AddRange(newLoggers);
        public void Add(params IDiagnoser[] newDiagnosers) => diagnosers.AddRange(newDiagnosers);
        public void Add(params IAnalyser[] newAnalysers) => analysers.AddRange(newAnalysers);
        public void Add(params IJob[] newJobs) => jobs.AddRange(newJobs);

        public void Add(IConfig config)
        {
            columns.AddRange(config.GetColumns());
            exporters.AddRange(config.GetExporters());
            loggers.AddRange(config.GetLoggers());
            diagnosers.AddRange(config.GetDiagnosers());
            analysers.AddRange(config.GetAnalysers());
            jobs.AddRange(config.GetJobs());
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
            }
            return manualConfig;
        }

        public static IConfig Parse(string[] args) => new ConfigParser().Parse(args);
    }
}