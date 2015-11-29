using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public static class BenchmarkPluginsExtensions
    {
        public static IBenchmarkPluginBuilder AddLoggers(this IBenchmarkPluginBuilder builder, params IBenchmarkLogger[] loggers)
        {
            foreach (var logger in loggers)
                builder.AddLogger(logger);
            return builder;
        }

        public static IBenchmarkPluginBuilder AddExporters(this IBenchmarkPluginBuilder builder, params IBenchmarkExporter[] exporters)
        {
            foreach (var exporter in exporters)
                builder.AddExporter(exporter);
            return builder;
        }

        public static IBenchmarkPluginBuilder AddDiagnoser(this IBenchmarkPluginBuilder builder, params IBenchmarkDiagnoser[] diagnosers)
        {
            foreach (var diagnoser in diagnosers)
                builder.AddDiagnoser(diagnoser);
            return builder;
        }
    }
}