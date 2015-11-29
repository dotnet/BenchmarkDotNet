using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public static class BenchmarkPluginsExtensions
    {
        public static void AddLoggers(this IBenchmarkPlugins plugins, params IBenchmarkLogger[] loggers)
        {
            foreach (var logger in loggers)
                plugins.AddLogger(logger);
        }

        public static void AddExporters(this IBenchmarkPlugins plugins, params IBenchmarkExporter[] exporters)
        {
            foreach (var exporter in exporters)
                plugins.AddExporter(exporter);
        }

        public static void AddDiagnoser(this IBenchmarkPlugins plugins, params IBenchmarkDiagnoser[] diagnosers)
        {
            foreach (var diagnoser in diagnosers)
                plugins.AddDiagnoser(diagnoser);
        }
    }
}