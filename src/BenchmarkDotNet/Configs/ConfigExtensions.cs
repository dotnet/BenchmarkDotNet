using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.EventProcessors;
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
    public static class ConfigExtensions
    {
        [PublicAPI] public static ManualConfig AddColumn(this IConfig config, params IColumn[] columns) => config.With(m => m.AddColumn(columns));

        [PublicAPI] public static ManualConfig AddColumnProvider(this IConfig config, params IColumnProvider[] columnProviders) => config.With(m => m.AddColumnProvider(columnProviders));

        [PublicAPI] public static ManualConfig AddLogger(this IConfig config, params ILogger[] loggers) => config.With(m => m.AddLogger(loggers));

        [PublicAPI] public static ManualConfig AddExporter(this IConfig config, params IExporter[] exporters) => config.With(m => m.AddExporter(exporters));

        [PublicAPI] public static ManualConfig AddDiagnoser(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.AddDiagnoser(diagnosers));

        [PublicAPI] public static ManualConfig AddAnalyser(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.AddAnalyser(analysers));

        [PublicAPI] public static ManualConfig AddValidator(this IConfig config, params IValidator[] validators) => config.With(m => m.AddValidator(validators));

        [PublicAPI] public static ManualConfig AddJob(this IConfig config, Job job) => config.With(m => m.AddJob(job));

        [PublicAPI] public static ManualConfig WithOrderer(this IConfig config, IOrderer orderer) => config.With(m => m.WithOrderer(orderer));

        [PublicAPI] public static ManualConfig AddHardwareCounters(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.AddHardwareCounters(counters));

        [PublicAPI] public static ManualConfig AddFilter(this IConfig config, params IFilter[] filters) => config.With(c => c.AddFilter(filters));

        [PublicAPI] public static ManualConfig WithSummaryStyle(this IConfig config, SummaryStyle summaryStyle) => config.With(c => c.WithSummaryStyle(summaryStyle));

        [PublicAPI] public static ManualConfig WithArtifactsPath(this IConfig config, string artifactsPath) => config.With(m => m.WithArtifactsPath(artifactsPath));
        [PublicAPI] public static ManualConfig WithUnionRule(this IConfig config, ConfigUnionRule unionRule) => config.With(m => m.WithUnionRule(unionRule));
        [PublicAPI] public static ManualConfig WithCultureInfo(this IConfig config, CultureInfo cultureInfo) => config.With(m => m.CultureInfo = cultureInfo);

        /// <summary>
        /// determines if all auto-generated files should be kept or removed after running the benchmarks
        /// </summary>
        [PublicAPI] public static IConfig KeepBenchmarkFiles(this IConfig config, bool value = true) => config.WithOption(ConfigOptions.KeepBenchmarkFiles, value);

        /// <summary>
        /// determines if the exported result files should not be overwritten (be default they are overwritten)
        /// </summary>

        [PublicAPI] public static IConfig DontOverwriteResults(this IConfig config, bool value = true) => config.WithOption(ConfigOptions.DontOverwriteResults, value);

        /// <summary>
        /// determines if benchmarking should be stopped after the first error (by default it's not)
        /// </summary>
        [PublicAPI] public static IConfig StopOnFirstError(this IConfig config, bool value = true) => config.WithOption(ConfigOptions.StopOnFirstError, value);

        /// <summary>
        /// sets given option to provided value
        /// </summary>
        [PublicAPI] public static ManualConfig WithOption(this IConfig config, ConfigOptions option, bool value) => config.With(m => m.WithOption(option, value));

        /// <summary>
        /// sets given options for the config
        /// </summary>
        [PublicAPI] public static ManualConfig WithOptions(this IConfig config, ConfigOptions options) => config.With(m => m.WithOptions(options));

        [PublicAPI] public static ManualConfig AddLogicalGroupRules(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => config.With(c => c.AddLogicalGroupRules(rules));
        [PublicAPI] public static ManualConfig AddEventProcessor(this IConfig config, params EventProcessor[] eventProcessors) => config.With(c => c.AddEventProcessor(eventProcessors));

        [PublicAPI] public static ManualConfig HideColumns(this IConfig config, params string[] columnNames) => config.With(c => c.HideColumns(columnNames));
        [PublicAPI] public static ManualConfig HideColumns(this IConfig config, params IColumn[] columns) => config.With(c => c.HideColumns(columns));
        [PublicAPI] public static ManualConfig HideColumns(this IConfig config, params IColumnHidingRule[] rules) => config.With(c => c.HideColumns(rules));

        public static ImmutableConfig CreateImmutableConfig(this IConfig config) => ImmutableConfigBuilder.Create(config);

        internal static ILogger GetNonNullCompositeLogger(this IConfig config)
        {
            // if user did not provide any loggers, we use the ConsoleLogger to somehow show the errors to the user
            if (config == null || !config.GetLoggers().Any())
                return new CompositeLogger(ImmutableHashSet.Create(ConsoleLogger.Default));

            return new CompositeLogger(config.GetLoggers().ToImmutableHashSet());
        }

        private static ManualConfig With(this IConfig config, Action<ManualConfig> addAction)
        {
            var manualConfig = ManualConfig.Create(config);
            addAction(manualConfig);
            return manualConfig;
        }
    }
}
