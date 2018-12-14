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
    public interface IConfig
    {
        IEnumerable<IColumnProvider> GetColumnProviders();
        IEnumerable<IExporter> GetExporters();
        IEnumerable<ILogger> GetLoggers();
        IEnumerable<IDiagnoser> GetDiagnosers();
        IEnumerable<IAnalyser> GetAnalysers();
        IEnumerable<Job> GetJobs();
        IEnumerable<IValidator> GetValidators();
        IEnumerable<HardwareCounter> GetHardwareCounters();
        IEnumerable<IFilter> GetFilters();

        [CanBeNull]
        IOrderer GetOrderer();
        ISummaryStyle GetSummaryStyle();

        ConfigUnionRule UnionRule { get; }

        /// <summary>
        /// determines if all auto-generated files should be kept or removed after running the benchmarks
        /// </summary>
        bool KeepBenchmarkFiles { get; }

        /// <summary>
        /// determines if all benchmarks results should be joined into a single summary or not
        /// </summary>
        bool SummaryPerType { get; }

        /// <summary>
        /// the default value is "./BenchmarkDotNet.Artifacts"
        /// </summary>
        string ArtifactsPath { get; }
        
        /// <summary>
        /// the default value is ASCII
        /// </summary>
        Encoding Encoding { get; }

        IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules();

        bool StopOnFirstError { get; }
    }
}