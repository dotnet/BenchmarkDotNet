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
        [PublicAPI] public static IConfig With(this IConfig config, params IColumn[] columns) => config.With(m => m.Add(columns));
        [PublicAPI] public static IConfig With(this IConfig config, params IColumnProvider[] columnProviders) => config.With(m => m.Add(columnProviders));
        [PublicAPI] public static IConfig With(this IConfig config, params ILogger[] loggers) => config.With(m => m.Add(loggers));
        [PublicAPI] public static IConfig With(this IConfig config, params IExporter[] exporters) => config.With(m => m.Add(exporters));
        [PublicAPI] public static IConfig With(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.Add(diagnosers));
        [PublicAPI] public static IConfig With(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.Add(analysers));
        [PublicAPI] public static IConfig With(this IConfig config, params IValidator[] validators) => config.With(m => m.Add(validators));
        [PublicAPI] public static IConfig With(this IConfig config, params Job[] jobs) => config.With(m => m.Add(jobs));
        [PublicAPI] public static IConfig With(this IConfig config, IOrderer provider) => config.With(m => m.Orderer = provider);
        [PublicAPI] public static IConfig With(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.Add(counters));
        [PublicAPI] public static IConfig With(this IConfig config, params IFilter[] filters) => config.With(c => c.Add(filters));
        [PublicAPI] public static IConfig With(this IConfig config, Encoding encoding) => config.With(c => c.Encoding = encoding);
        [PublicAPI] public static IConfig With(this IConfig config, SummaryStyle summaryStyle) => config.With(c => c.SummaryStyle = summaryStyle);

        /// <summary>
        /// determines if all auto-generated files should be kept or removed after running the benchmarks
        /// </summary>
        [PublicAPI] public static IConfig KeepBenchmarkFiles(this IConfig config, bool value = true) => config.With(m => m.Options = m.Options.Set(value, ConfigOptions.KeepBenchmarkFiles));
        /// <summary>
        /// determines if benchmarking should be stopped after the first error (by default it's not)
        /// </summary>
        [PublicAPI] public static IConfig StopOnFirstError(this IConfig config, bool value = true) => config.With(m => m.Options = m.Options.Set(value, ConfigOptions.StopOnFirstError));
        [PublicAPI] public static IConfig WithArtifactsPath(this IConfig config, string artifactsPath) => config.With(m => m.ArtifactsPath = artifactsPath);
        /// <summary>
        /// sets given options for the config
        /// </summary>
        [PublicAPI] public static IConfig With(this IConfig config, ConfigOptions options) => config.With(m => m.Options = options);
        [PublicAPI] public static IConfig With(this IConfig config, params BenchmarkLogicalGroupRule[] rules) => config.With(c => c.Add(rules));

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