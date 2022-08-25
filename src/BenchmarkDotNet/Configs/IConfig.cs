using System;
using System.Collections.Generic;
using System.Globalization;
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
        IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules();
        IEnumerable<IColumnHidingRule> GetColumnHidingRules();

        [CanBeNull] IOrderer Orderer { get; }

        SummaryStyle SummaryStyle { get; }

        ConfigUnionRule UnionRule { get; }

        /// <summary>
        /// the default value is "./BenchmarkDotNet.Artifacts"
        /// </summary>
        string ArtifactsPath { get; }

        [CanBeNull]
        CultureInfo CultureInfo { get; }

        /// <summary>
        /// a set of custom flags that can enable/disable various settings
        /// </summary>
        ConfigOptions Options { get; }

        /// <summary>
        /// the auto-generated project build timeout
        /// </summary>
        TimeSpan BuildTimeout { get; }

        /// <summary>
        /// Collect any errors or warnings when composing the configuration
        /// </summary>
        IReadOnlyList<Conclusion> ConfigAnalysisConclusion { get; }
    }
}
