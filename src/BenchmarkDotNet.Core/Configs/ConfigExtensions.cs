using System;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using RunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.Configs
{
    public static class ConfigExtensions
    {
        public static ILogger GetCompositeLogger(this IConfig config) => new CompositeLogger(config.GetLoggers().ToArray());
        public static IExporter GetCompositeExporter(this IConfig config) => new CompositeExporter(config.GetExporters().ToArray());
        public static IDiagnoser GetCompositeDiagnoser(this IConfig config) => new CompositeDiagnoser(config.GetDiagnosers().ToArray());
        public static IDiagnoser GetCompositeDiagnoser(this IConfig config, Benchmark benchmark, RunMode runMode) => new CompositeDiagnoser(config.GetDiagnosers().Where(d => d.GetRunMode(benchmark) == runMode).ToArray());
        public static IAnalyser GetCompositeAnalyser(this IConfig config) => new CompositeAnalyser(config.GetAnalysers().ToArray());
        public static IValidator GetCompositeValidator(this IConfig config) => new CompositeValidator(config.GetValidators().ToArray());

        public static IConfig With(this IConfig config, params IColumn[] columns) => config.With(m => m.Add(columns));
        public static IConfig With(this IConfig config, params IColumnProvider[] columnProviders) => config.With(m => m.Add(columnProviders));
        public static IConfig With(this IConfig config, params ILogger[] loggers) => config.With(m => m.Add(loggers));
        public static IConfig With(this IConfig config, params IExporter[] exporters) => config.With(m => m.Add(exporters));
        public static IConfig With(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.Add(diagnosers));
        public static IConfig With(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.Add(analysers));
        public static IConfig With(this IConfig config, params IValidator[] validators) => config.With(m => m.Add(validators));
        public static IConfig With(this IConfig config, params Job[] jobs) => config.With(m => m.Add(jobs));
        public static IConfig With(this IConfig config, IOrderProvider provider) => config.With(m => m.Set(provider));
        public static IConfig With(this IConfig config, params HardwareCounter[] counters) => config.With(c => c.Add(counters));
        public static IConfig With(this IConfig config, params IFilter[] filters) => config.With(c => c.Add(filters));

        public static IConfig KeepBenchmarkFiles(this IConfig config, bool value = true) => config.With(m => m.KeepBenchmarkFiles = value);
        public static IConfig RemoveBenchmarkFiles(this IConfig config) => config.KeepBenchmarkFiles(false);

        private static IConfig With(this IConfig config, Action<ManualConfig> addAction)
        {
            var manualConfig = ManualConfig.Create(config);
            addAction(manualConfig);
            return manualConfig;
        }
    }
}