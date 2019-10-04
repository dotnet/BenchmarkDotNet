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
        [PublicAPI] public static IConfig With(this IConfig config, params IColumn[] columns) => AddColumn(config, columns);
        [PublicAPI] public static IConfig AddColumn(this IConfig config, params IColumn[] columns) => config.With(m => m.AddColumn(columns));

        [Obsolete("This property will soon be removed, please start using AddColumnProvider instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IColumnProvider[] columnProviders) => AddColumnProvider(config, columnProviders);
        [PublicAPI] public static IConfig AddColumnProvider(this IConfig config, params IColumnProvider[] columnProviders) => config.With(m => m.AddColumnProvider(columnProviders));

        [Obsolete("This property will soon be removed, please start using AddLogger instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params ILogger[] loggers) => AddLogger(config, loggers);
        [PublicAPI] public static IConfig AddLogger(this IConfig config, params ILogger[] loggers) => config.With(m => m.AddLogger(loggers));

        [Obsolete("This property will soon be removed, please start using AddExporter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IExporter[] exporters) => AddExporter(config, exporters);
        [PublicAPI] public static IConfig AddExporter(this IConfig config, params IExporter[] exporters) => config.With(m => m.AddExporter(exporters));

        [Obsolete("This property will soon be removed, please start using AddDiagnoser instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IDiagnoser[] diagnosers) => AddDiagnoser(config, diagnosers);
        [PublicAPI] public static IConfig AddDiagnoser(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.AddDiagnoser(diagnosers));

        [Obsolete("This property will soon be removed, please start using AddAnalyser instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IAnalyser[] analysers) => AddAnalyser(config, analysers);
        [PublicAPI] public static IConfig AddAnalyser(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.AddAnalyser(analysers));

        [Obsolete("This property will soon be removed, please start using AddValidator instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IValidator[] validators) => AddValidator(config, validators);
        [PublicAPI] public static IConfig AddValidator(this IConfig config, params IValidator[] validators) => config.With(m => m.AddValidator(validators));

        [Obsolete("This property will soon be removed, please start using AddJob instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params Job[] jobs) => AddJob(config, jobs);
        [PublicAPI] public static IConfig AddJob(this IConfig config, params Job[] jobs) => config.With(m => m.AddJob(jobs));

        [Obsolete("This property will soon be removed, please start using WithOrderer instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, IOrderer provider) => WithOrderer(config, provider);
        [PublicAPI] public static IConfig WithOrderer(this IConfig config, IOrderer provider) => config.With(m => m.Orderer = provider);

        [Obsolete("This property will soon be removed, please start using AddHardwareCounter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.AddHardwareCounter(counters));
        [PublicAPI] public static IConfig AddHardwareCounter(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.AddHardwareCounter(counters));

        [Obsolete("This property will soon be removed, please start using AddFilter instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params IFilter[] filters) => AddFilter(config, filters);
        [PublicAPI] public static IConfig AddFilter(this IConfig config, params IFilter[] filters) => config.With(c => c.AddFilter(filters));

        [Obsolete("This property will soon be removed, please start using WithEncoding instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, Encoding encoding) => WithEncoding(config, encoding);
        [PublicAPI] public static IConfig WithEncoding(this IConfig config, Encoding encoding) => config.With(c => c.Encoding = encoding);

        [Obsolete("This property will soon be removed, please start using WithSummaryStyle instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, SummaryStyle summaryStyle) => WithSummaryStyle(config, summaryStyle);
        [PublicAPI] public static IConfig WithSummaryStyle(this IConfig config, SummaryStyle summaryStyle) => config.With(c => c.SummaryStyle = summaryStyle);
        [PublicAPI] public static IConfig WithArtifactsPath(this IConfig config, string artifactsPath) => config.With(m => m.ArtifactsPath = artifactsPath);

        /// <summary>
        /// determines if all auto-generated files should be kept or removed after running the benchmarks
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig KeepBenchmarkFiles(this IConfig config, bool value = true) => WithOptionsIf(config, value, ConfigOptions.KeepBenchmarkFiles);
        /// <summary>
        /// determines if the exported result files should not be overwritten (be default they are overwritten)
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig DontOverwriteResults(this IConfig config, bool value = true) => WithOptionsIf(config, value, ConfigOptions.DontOverwriteResults);
        /// <summary>
        /// determines if benchmarking should be stopped after the first error (by default it's not)
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptionsIf instead.")]
        [PublicAPI] public static IConfig StopOnFirstError(this IConfig config, bool value = true) => WithOptionsIf(config, value, ConfigOptions.StopOnFirstError);

        /// <summary>
        /// sets given options for the config
        /// </summary>
        [Obsolete("This property will soon be removed, please start using WithOptions instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, ConfigOptions options) => WithOptions(config, options);
        [PublicAPI] public static IConfig WithOptions(this IConfig config, ConfigOptions options) => config.With(m => m.Options = options);
        [PublicAPI] public static IConfig WithOptionsIf(this IConfig config, bool value, ConfigOptions options) => config.With(m => m.Options.Set(value, options));

        [Obsolete("This property will soon be removed, please start using GroupBenchmarksBy instead.")]
        [PublicAPI] public static IConfig With(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => GroupBenchmarksBy(config, rules);
        [PublicAPI] public static IConfig GroupBenchmarksBy(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => config.With(c => c.GroupBenchmarksBy(rules));

        public static ImmutableConfig CreateImmutableConfig(this IConfig config) => ImmutableConfigBuilder.Create(config);

        internal static ILogger GetNonNullCompositeLogger(this IConfig config)
        {
            // if user did not provide any loggers, we use the ConsoleLogger to somehow show the errors to the user
            if (config == null || !config.GetLoggers().Any())
                return new CompositeLogger(ImmutableHashSet.Create(ConsoleLogger.Default));

            return new CompositeLogger(config.GetLoggers().ToImmutableHashSet());
        }

        private static IConfig With(this IConfig config, Action<ManualConfig> addAction)
        {
            var manualConfig = ManualConfig.Create(config);
            addAction(manualConfig);
            return manualConfig;
        }
    }
}