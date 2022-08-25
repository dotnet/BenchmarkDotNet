using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Configs
{
    public class DefaultConfig : IConfig
    {
        public static readonly IConfig Instance = new DefaultConfig();
        private readonly static Conclusion[] emptyConclusion = Array.Empty<Conclusion>();

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
            if (LinqPadLogger.IsAvailable)
                yield return LinqPadLogger.Instance;
            else
                yield return ConsoleLogger.Default;
        }

        public IEnumerable<IAnalyser> GetAnalysers()
        {
            yield return EnvironmentAnalyser.Default;
            yield return OutliersAnalyser.Default;
            yield return MinIterationTimeAnalyser.Default;
            yield return MultimodalDistributionAnalyzer.Default;
            yield return RuntimeErrorAnalyser.Default;
            yield return ZeroMeasurementAnalyser.Default;
            yield return BaselineCustomAnalyzer.Default;
            yield return HideColumnsAnalyser.Default;
        }

        public IEnumerable<IValidator> GetValidators()
        {
            yield return BaselineValidator.FailOnError;
            yield return SetupCleanupValidator.FailOnError;
#if !DEBUG
            yield return JitOptimizationsValidator.FailOnError;
#endif
            yield return RunModeValidator.FailOnError;
            yield return GenericBenchmarksValidator.DontFailOnError;
            yield return DeferredExecutionValidator.FailOnError;
            yield return ParamsAllValuesValidator.FailOnError;
        }

        public IOrderer Orderer => null;

        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;

        public CultureInfo CultureInfo => null;

        public ConfigOptions Options => ConfigOptions.Default;

        public SummaryStyle SummaryStyle => SummaryStyle.Default;

        public TimeSpan BuildTimeout => TimeSpan.FromSeconds(120);

        public string ArtifactsPath
        {
            get
            {
                var root = RuntimeInformation.IsAndroid() ?
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) :
                    Directory.GetCurrentDirectory();
                return Path.Combine(root, "BenchmarkDotNet.Artifacts");
            }
        }

        public IReadOnlyList<Conclusion> ConfigAnalysisConclusion => emptyConclusion;

        public IEnumerable<Job> GetJobs() => Array.Empty<Job>();

        public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() => Array.Empty<BenchmarkLogicalGroupRule>();

        public IEnumerable<IDiagnoser> GetDiagnosers() => Array.Empty<IDiagnoser>();

        public IEnumerable<HardwareCounter> GetHardwareCounters() => Array.Empty<HardwareCounter>();

        public IEnumerable<IFilter> GetFilters() => Array.Empty<IFilter>();

        public IEnumerable<IColumnHidingRule> GetColumnHidingRules() => Array.Empty<IColumnHidingRule>();
    }
}