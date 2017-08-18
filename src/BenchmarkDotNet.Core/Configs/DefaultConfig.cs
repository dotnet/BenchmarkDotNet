using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Configs
{
    public class DefaultConfig : IConfig
    {
        public static readonly IConfig Instance = new DefaultConfig();

        private DefaultConfig()
        {
        }

        public IEnumerable<IColumnProvider> GetColumnProviders() => DefaultColumnProviders.Instance;

        public IEnumerable<IExporter> GetExporters()
        {
            // Now that we can specify exporters on the cmd line (e.g. "exporters=html,stackoverflow"), 
            // we should have less enabled by default and then users can turn on the ones they want
            yield return CsvExporter.Default;
            yield return MarkdownExporter.GitHub;
            yield return HtmlExporter.Default;
        }

        public IEnumerable<ILogger> GetLoggers()
        {
            yield return ConsoleLogger.Default;
        }

        public IEnumerable<IAnalyser> GetAnalysers()
        {
            yield return EnvironmentAnalyser.Default;
            yield return OutliersAnalyser.Default;
            yield return MinIterationTimeAnalyser.Default;
            yield return IterationSetupCleanupAnalyser.Default;
        }

        public IEnumerable<IValidator> GetValidators()
        {
            yield return BaselineValidator.FailOnError;
            yield return SetupCleanupValidator.FailOnError;
            yield return JitOptimizationsValidator.DontFailOnError;
            yield return UnrollFactorValidator.Default;
        }

        public IEnumerable<Job> GetJobs() => Array.Empty<Job>();

        public IOrderProvider GetOrderProvider() => null;

        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;

        public bool KeepBenchmarkFiles => false;

        public ISummaryStyle GetSummaryStyle() => SummaryStyle.Default;

        public IEnumerable<IDiagnoser> GetDiagnosers() => Array.Empty<IDiagnoser>();

        public IEnumerable<HardwareCounter> GetHardwareCounters() => Array.Empty<HardwareCounter>();

        public IEnumerable<IFilter> GetFilters() => Array.Empty<IFilter>();
    }
}