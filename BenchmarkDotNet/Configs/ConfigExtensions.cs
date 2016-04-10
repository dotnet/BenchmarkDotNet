using System;
using System.Linq;
using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Configs
{
    public static class ConfigExtensions
    {
        public static ILogger GetCompositeLogger(this IConfig config) => new CompositeLogger(config.GetLoggers().ToArray());
        public static IExporter GetCompositeExporter(this IConfig config) => new CompositeExporter(config.GetExporters().ToArray());
        public static IDiagnoser GetCompositeDiagnoser(this IConfig config) => new CompositeDiagnoser(config.GetDiagnosers().ToArray());
        public static IAnalyser GetCompositeAnalyser(this IConfig config) => new CompositeAnalyser(config.GetAnalysers().ToArray());

        public static IConfig With(this IConfig config, params IColumn[] columns) => config.With(m => m.Add(columns));
        public static IConfig With(this IConfig config, params ILogger[] loggers) => config.With(m => m.Add(loggers));
        public static IConfig With(this IConfig config, params IExporter[] exporters) => config.With(m => m.Add(exporters));
        public static IConfig With(this IConfig config, params IDiagnoser[] diagnosers) => config.With(m => m.Add(diagnosers));
        public static IConfig With(this IConfig config, params IAnalyser[] analysers) => config.With(m => m.Add(analysers));
        public static IConfig With(this IConfig config, params IJob[] jobs) => config.With(m => m.Add(jobs));
        public static IConfig With(this IConfig config, IOrderProvider provider) => config.With(m => m.Set(provider));

        private static IConfig With(this IConfig config, Action<ManualConfig> addAction)
        {
            var manualConfig = ManualConfig.Create(config);
            addAction(manualConfig);
            return manualConfig;
        }
    }
}