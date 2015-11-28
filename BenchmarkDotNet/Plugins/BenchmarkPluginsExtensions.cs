using BenchmarkDotNet.Plugins.Diagnosters;
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

        public static void AddDiagnoster(this IBenchmarkPlugins plugins, params IBenchmarkDiagnoster[] diagnosters)
        {
            foreach (var diagnoster in diagnosters)
                plugins.AddDiagnoster(diagnoster);
        }
    }
}