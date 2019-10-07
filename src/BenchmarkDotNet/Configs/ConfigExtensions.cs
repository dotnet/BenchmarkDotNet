using System;
using System.Collections.Immutable;
using System.Linq;
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
    public static class ConfigExtensions
    {
        [Obsolete("This property will soon be removed, please start using AddColumn instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IColumn[] columns) => config.AddColumn(columns);
        [PublicAPI] public static ManualConfig AddColumn(this IConfig config, params IColumn[] columns) => config.With(m => m.AddColumn(columns));

        [Obsolete("This property will soon be removed, please start using AddColumnProvider instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IColumnProvider[] columnProviders) => config.AddColumnProvider(columnProviders);
        [PublicAPI] public static ManualConfig AddColumnProvider(this IConfig config, params IColumnProvider[] columnProviders) => config.With(m => m.AddColumnProvider(columnProviders));

        [Obsolete("This property will soon be removed, please start using AddLogger instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params ILogger[] loggers) => config.AddLogger(loggers);
        [PublicAPI] public static ManualConfig AddLogger(this IConfig config, params ILogger[] loggers) => config.With(m => m.AddLogger(loggers));

        [Obsolete("This property will soon be removed, please start using AddExporter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IExporter[] exporters) => config.AddExporter(exporters);
        [PublicAPI] public static ManualConfig AddExporter(this IConfig config, params IExporter[] exporters) => config.With(m => m.AddExporter(exporters));

        [Obsolete("This property will soon be removed, please start using AddDiagnoser instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IDiagnoser[] diagnosers) => config.AddDiagnoser(diagnosers);
        [PublicAPI] public static ManualConfig AddDiagnoser(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.AddDiagnoser(diagnosers));

        [Obsolete("This property will soon be removed, please start using AddAnalyser instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IAnalyser[] analysers) => config.AddAnalyser(analysers);
        [PublicAPI] public static ManualConfig AddAnalyser(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.AddAnalyser(analysers));

        [Obsolete("This property will soon be removed, please start using AddValidator instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IValidator[] validators) => config.AddValidator(validators);
        [PublicAPI] public static ManualConfig AddValidator(this IConfig config, params IValidator[] validators) => config.With(m => m.AddValidator(validators));

        [Obsolete("This property will soon be removed, please start using AddJob instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params Job[] jobs) => config.AddJob(jobs);
        [PublicAPI] public static ManualConfig AddJob(this IConfig config, params Job[] jobs) => config.With(m => m.AddJob(jobs));

        [Obsolete("This property will soon be removed, please start using WithOrderer instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, IOrderer orderer) => config.WithOrderer(orderer);
        [PublicAPI] public static ManualConfig WithOrderer(this IConfig config, IOrderer orderer) => config.With(m => m.WithOrderer(orderer));

        [Obsolete("This property will soon be removed, please start using AddHardwareCounter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.AddHardwareCounter(counters));
        [PublicAPI] public static ManualConfig AddHardwareCounter(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.AddHardwareCounter(counters));

        [Obsolete("This property will soon be removed, please start using AddFilter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IFilter[] filters) => config.AddFilter(filters);
        [PublicAPI] public static ManualConfig AddFilter(this IConfig config, params IFilter[] filters) => config.With(c => c.AddFilter(filters));

        [Obsolete("This property will soon be removed, please start using WithEncoding instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, Encoding encoding) => config.WithEncoding( encoding);
        [PublicAPI] public static ManualConfig WithEncoding(this IConfig config, Encoding encoding) => config.With(c => c.WithEncoding(encoding));

        [Obsolete("This property will soon be removed, please start using WithSummaryStyle instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, SummaryStyle summaryStyle) => config.WithSummaryStyle(summaryStyle);
        [PublicAPI] public static ManualConfig WithSummaryStyle(this IConfig config, SummaryStyle summaryStyle) => config.With(c => c.WithSummaryStyle(summaryStyle));

        [PublicAPI] public static ManualConfig WithArtifactsPath(this IConfig config, string artifactsPath) => config.With(m => m.WithArtifactsPath(artifactsPath));
        [PublicAPI] public static ManualConfig WithUnionRule(this IConfig config, ConfigUnionRule unionRule) => config.With(m => m.WithUnionRule(unionRule));

        /// <summary>
        /// determines if all auto-generated files should be kept or removed after running the benchmarks
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig KeepBenchmarkFiles(this IConfig config, bool value = true) => config.WithOptionsIf(value, ConfigOptions.KeepBenchmarkFiles);
        /// <summary>
        /// determines if the exported result files should not be overwritten (be default they are overwritten)
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig DontOverwriteResults(this IConfig config, bool value = true) => config.WithOptionsIf(value, ConfigOptions.DontOverwriteResults);
        /// <summary>
        /// determines if benchmarking should be stopped after the first error (by default it's not)
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig StopOnFirstError(this IConfig config, bool value = true) => config.WithOptionsIf(value, ConfigOptions.StopOnFirstError);

        /// <summary>
        /// sets given options for the config
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptions instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, ConfigOptions options) => config.WithOptions(options);
        [PublicAPI] public static ManualConfig WithOptions(this IConfig config, ConfigOptions options) => config.With(m => m.WithOptions(options));
        [PublicAPI] public static ManualConfig WithoutOptions(this IConfig config, ConfigOptions options) => config.With(m => m.WithoutOptions(options));
        [PublicAPI] public static ManualConfig WithOptionsIf(this IConfig config, bool value, ConfigOptions options) => config.With(m => m.WithOptionsIf(value, options));
        

        [Obsolete("This property will soon be removed, please start using WithLogicalGroupRules instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => config.WithLogicalGroupRules(rules);
        [PublicAPI] public static ManualConfig WithLogicalGroupRules(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => config.With(c => c.WithLogicalGroupRules(rules));

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